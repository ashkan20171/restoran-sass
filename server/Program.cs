using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<RestaurantDb>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Restaurant") ?? throw new InvalidOperationException("Set ConnectionStrings:Restaurant")));
builder.Services.AddIdentityApiEndpoints<IdentityUser>(o => { o.Password.RequiredLength = 12; o.User.RequireUniqueEmail = true; }).AddRoles<IdentityRole>().AddEntityFrameworkStores<RestaurantDb>();
builder.Services.AddAuthorization(o => { o.AddPolicy("AdminOnly", p => p.RequireRole("Admin")); o.AddPolicy("KitchenAccess", p => p.RequireRole("Admin", "Manager", "Kitchen")); o.AddPolicy("ReceptionAccess", p => p.RequireRole("Admin", "Manager", "Reception")); });
builder.Services.ConfigureApplicationCookie(o => { o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.LoginPath = "/staff.html"; });
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("public", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
builder.Services.AddHttpClient("ai", c => { c.BaseAddress = new Uri("https://api.openai.com/"); c.Timeout = TimeSpan.FromSeconds(20); });
var app = builder.Build();
// Bootstrap an administrator only when explicit environment variables are provided.
using (var scope = app.Services.CreateScope()) {
  var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
  var email = config["BOOTSTRAP_ADMIN_EMAIL"]; var password = config["BOOTSTRAP_ADMIN_PASSWORD"];
  if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)) {
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    if (!await roles.RoleExistsAsync("Admin")) await roles.CreateAsync(new IdentityRole("Admin"));
    var existing = await users.FindByEmailAsync(email);
    if (existing is null) {
      var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
      var result = await users.CreateAsync(user, password);
      if (!result.Succeeded) throw new InvalidOperationException("Admin bootstrap failed: " + string.Join(", ", result.Errors.Select(x => x.Description)));
      await users.AddToRoleAsync(user, "Admin");
    }
  }
}
app.UseHttpsRedirection(); app.UseDefaultFiles(); app.UseStaticFiles(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
// Identity endpoints are intentionally NOT mapped publicly. Admin users are provisioned via the CLI tool below.
app.MapPost("/api/admin/login", async (SignInManager<IdentityUser> signin, LoginInput input) => {
  if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password)) return Results.BadRequest();
  var result = await signin.PasswordSignInAsync(input.Email, input.Password, false, lockoutOnFailure:true);
  return result.Succeeded ? Results.Ok(new { message="Signed in" }) : Results.Unauthorized();
}).RequireRateLimiting("public");
// Authenticated role discovery: UI uses this for presentation, API policies remain authoritative.
app.MapGet("/api/admin/me", async (System.Security.Claims.ClaimsPrincipal principal, UserManager<IdentityUser> users) => {
  var user = await users.GetUserAsync(principal);
  if (user is null) return Results.Unauthorized();
  return Results.Ok(new { Email = user.Email, Roles = await users.GetRolesAsync(user) });
}).RequireAuthorization();
app.MapPost("/api/admin/logout", async (HttpRequest request, SignInManager<IdentityUser> signin) => { if (!IsSameOriginWrite(request)) return Results.StatusCode(403); await signin.SignOutAsync(); return Results.Ok(); }).RequireAuthorization();
app.MapPost("/api/reservations", async (RestaurantDb db, ReservationInput input) => {
  if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 120 || string.IsNullOrWhiteSpace(input.Phone) || input.Phone.Length > 30 || input.Guests is < 1 or > 12 || input.Date < DateOnly.FromDateTime(DateTime.Today) || input.Date > DateOnly.FromDateTime(DateTime.Today.AddMonths(6)) || input.Note?.Length > 1000) return Results.ValidationProblem(new Dictionary<string,string[]> { ["reservation"] = ["Invalid reservation details"] });
  var r = new Reservation { Name=input.Name.Trim(), Phone=input.Phone.Trim(), Date=input.Date, Time=input.Time, Guests=input.Guests, Note=input.Note?.Trim() ?? "", Status="Pending" };
  db.Reservations.Add(r); await db.SaveChangesAsync(); return Results.Created($"/api/reservations/{r.Id}", new { r.Id, r.Status, message="Request received, not confirmed" });
}).RequireRateLimiting("public");
app.MapGet("/api/admin/reservations", async (RestaurantDb db) => await db.Reservations.AsNoTracking().OrderByDescending(r => r.Id).Take(200).ToListAsync()).RequireAuthorization("ReceptionAccess");
app.MapGet("/api/menu", async (RestaurantDb db) => await db.MenuItems.AsNoTracking().Where(x=>x.Active).OrderBy(x=>x.Id).ToListAsync());
app.MapPost("/api/assistant", async (ChatInput input, IHttpClientFactory factory, IConfiguration config, CancellationToken ct) => {
  if (string.IsNullOrWhiteSpace(input.Message) || input.Message.Length > 500 || input.Language is not ("en" or "fa")) return Results.BadRequest();
  var key=config["OPENAI_API_KEY"]; if(string.IsNullOrWhiteSpace(key))return Results.Json(new { error="AI service not configured" },statusCode:503);
  var client=factory.CreateClient("ai"); using var request=new HttpRequestMessage(HttpMethod.Post,"v1/chat/completions");request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
  request.Content=JsonContent.Create(new { model=config["AI_MODEL"] ?? "gpt-4o-mini", messages=new object[]{new {role="system",content="You are Ashkan Restaurant's bilingual assistant. Reply in requested language. Never invent confirmed reservations, menu prices, addresses, allergens, or opening hours. Direct users to staff for verified details. Never request payment card details."},new {role="user",content=$"Language: {input.Language}. Question: {input.Message}"}}, max_tokens=220 });
  using var response=await client.SendAsync(request,ct); if(!response.IsSuccessStatusCode)return Results.Json(new {error="AI provider unavailable"},statusCode:502);
  using var doc=await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct),cancellationToken:ct);
  var answer=doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();return Results.Ok(new { answer });
}).RequireRateLimiting("public");
app.MapPatch("/api/admin/reservations/{id:int}", async (int id, StatusInput input, HttpRequest request, RestaurantDb db) => {
  if (!IsSameOriginWrite(request)) return Results.StatusCode(403);
  if (input.Status is not ("Pending" or "Confirmed" or "Rejected")) return Results.BadRequest(new {error="Invalid status"});
  var item=await db.Reservations.FindAsync(id); if(item is null) return Results.NotFound();
  if (item.Status != input.Status) { item.Status=input.Status; db.AuditEntries.Add(new AuditEntry {ActorId=request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown",Action="ReservationStatusChanged",Target=$"reservation:{id}:{input.Status}"}); }
  await db.SaveChangesAsync(); return Results.Ok(new {item.Id,item.Status});
}).RequireAuthorization("ReceptionAccess");
app.MapGet("/api/admin/menu", async (RestaurantDb db) => await db.MenuItems.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()).RequireAuthorization("AdminOnly");
app.MapPost("/api/admin/menu", async (MenuInput input, HttpRequest request, RestaurantDb db) => {
  if(!IsSameOriginWrite(request))return Results.StatusCode(403);
  if(!ValidMenu(input))return Results.BadRequest(new {error="Invalid menu item"});
  var item=new MenuItem{NameEn=input.NameEn.Trim(),NameFa=input.NameFa.Trim(),Usd=input.Usd,Toman=input.Toman,Active=input.Active};
  db.MenuItems.Add(item);await db.SaveChangesAsync();
  db.AuditEntries.Add(new AuditEntry { ActorId=request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown", Action="MenuItemCreated", Target=$"menu:{item.Id}" });
  await db.SaveChangesAsync();return Results.Created($"/api/menu/{item.Id}",item);
}).RequireAuthorization("AdminOnly");
app.MapPut("/api/admin/menu/{id:int}", async (int id, MenuInput input, HttpRequest request, RestaurantDb db) => {
  if(!IsSameOriginWrite(request))return Results.StatusCode(403);
  if(!ValidMenu(input))return Results.BadRequest(new {error="Invalid menu item"});
  var item=await db.MenuItems.FindAsync(id);if(item is null)return Results.NotFound();
  item.NameEn=input.NameEn.Trim();item.NameFa=input.NameFa.Trim();item.Usd=input.Usd;item.Toman=input.Toman;item.Active=input.Active;
  db.AuditEntries.Add(new AuditEntry { ActorId=request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown", Action="MenuItemUpdated", Target=$"menu:{id}" });
  await db.SaveChangesAsync();return Results.Ok(item);
}).RequireAuthorization("AdminOnly");
app.MapGet("/api/admin/summary", async (RestaurantDb db) => Results.Ok(new {
  reservations=await db.Reservations.CountAsync(), pending=await db.Reservations.CountAsync(x=>x.Status=="Pending"),
  confirmed=await db.Reservations.CountAsync(x=>x.Status=="Confirmed"), activeDishes=await db.MenuItems.CountAsync(x=>x.Active)
})).RequireAuthorization("AdminOnly");
// Stage 8: order workflow. All monetary values are calculated from database menu prices.
app.MapPost("/api/orders", async (OrderInput input, RestaurantDb db) => {
  if (string.IsNullOrWhiteSpace(input.Customer) || input.Customer.Length > 120 || input.Items is null || input.Items.Count is < 1 or > 30 || input.Items.Any(x => x.Quantity is < 1 or > 20) || input.Currency is not ("USD" or "TOMAN")) return Results.BadRequest(new {error="Invalid order"});
  var ids=input.Items.Select(x=>x.MenuItemId).Distinct().ToArray();
  if(ids.Length!=input.Items.Count) return Results.BadRequest(new {error="Duplicate menu items"});
  var menu=await db.MenuItems.AsNoTracking().Where(x=>ids.Contains(x.Id)&&x.Active).ToDictionaryAsync(x=>x.Id);
  if(menu.Count!=ids.Length) return Results.BadRequest(new {error="Unavailable menu item"});
  await using var transaction = await db.Database.BeginTransactionAsync();
  foreach(var line in input.Items.OrderBy(x=>x.MenuItemId)) {
    var item=menu[line.MenuItemId];
    if(item.StockQuantity is null) continue;
    var updated=await db.MenuItems.Where(x=>x.Id==line.MenuItemId && x.StockQuantity>=line.Quantity)
      .ExecuteUpdateAsync(setters=>setters.SetProperty(x=>x.StockQuantity,x=>x.StockQuantity-line.Quantity));
    if(updated!=1) { await transaction.RollbackAsync(); return Results.Conflict(new {error="Insufficient stock",menuItemId=line.MenuItemId}); }
  }
  var order=new RestaurantOrder {Customer=input.Customer.Trim(),Currency=input.Currency,Status="Pending",CreatedAt=DateTimeOffset.UtcNow};
  foreach(var line in input.Items){var item=menu[line.MenuItemId]; var price=input.Currency=="USD"?item.Usd:item.Toman;order.Lines.Add(new RestaurantOrderLine {MenuItemId=item.Id,Name=item.NameEn,Quantity=line.Quantity,UnitPrice=price});}
  order.Total=order.Lines.Sum(x=>x.UnitPrice*x.Quantity);
  db.Orders.Add(order);await db.SaveChangesAsync();await transaction.CommitAsync();return Results.Created($"/api/orders/{order.Id}",new {order.Id,order.Status,order.Currency,order.Total});
}).RequireRateLimiting("public");
app.MapGet("/api/admin/orders",async (RestaurantDb db)=>await db.Orders.AsNoTracking().Include(x=>x.Lines).OrderByDescending(x=>x.Id).Take(100).ToListAsync()).RequireAuthorization("KitchenAccess");
app.MapPatch("/api/admin/orders/{id:int}",async (int id,StatusInput input,HttpRequest request,RestaurantDb db)=>{
 if(!IsSameOriginWrite(request))return Results.StatusCode(403);
 if(input.Status is not ("Pending" or "Preparing" or "Ready" or "Completed" or "Cancelled"))return Results.BadRequest();
 var order=await db.Orders.FindAsync(id);if(order is null)return Results.NotFound();
 var transitions=new Dictionary<string,string[]> { ["Pending"]=["Preparing","Cancelled"], ["Preparing"]=["Ready","Cancelled"], ["Ready"]=["Completed"], ["Completed"]=[], ["Cancelled"]=[] };
 if(!transitions.GetValueOrDefault(order.Status,[]).Contains(input.Status))return Results.Conflict(new {error="Invalid status transition"});
 if(input.Status=="Cancelled") {
   await using var transaction=await db.Database.BeginTransactionAsync();
   var lines=await db.Set<RestaurantOrderLine>().Where(x=>x.RestaurantOrderId==id).ToListAsync();
   foreach(var line in lines) await db.MenuItems.Where(x=>x.Id==line.MenuItemId && x.StockQuantity!=null)
      .ExecuteUpdateAsync(setters=>setters.SetProperty(x=>x.StockQuantity,x=>x.StockQuantity+line.Quantity));
   order.Status=input.Status;db.AuditEntries.Add(new AuditEntry { ActorId=request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown", Action="OrderStatusChanged", Target=$"order:{id}:{input.Status}" });await db.SaveChangesAsync();await transaction.CommitAsync();
 } else {order.Status=input.Status;db.AuditEntries.Add(new AuditEntry { ActorId=request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown", Action="OrderStatusChanged", Target=$"order:{id}:{input.Status}" });await db.SaveChangesAsync();}
 return Results.Ok(new {order.Id,order.Status});
}).RequireAuthorization("KitchenAccess");
// Stage 9: separate currency reporting; never combine USD and TOMAN totals.
app.MapGet("/api/admin/sales-summary", async (RestaurantDb db, CancellationToken ct) => {
  var orders = await db.Orders.AsNoTracking().Where(x => x.Status != "Cancelled")
    .GroupBy(x => new { x.Currency, x.Status })
    .Select(g => new { g.Key.Currency, g.Key.Status, Count = g.Count(), Total = g.Sum(x => x.Total) })
    .ToListAsync(ct);
  return Results.Ok(new { GeneratedAt = DateTimeOffset.UtcNow, Groups = orders });
}).RequireAuthorization("AdminOnly");
// Stage 10: bounded operational analytics, scoped to authenticated administrators.
app.MapGet("/api/admin/analytics", async (RestaurantDb db, CancellationToken ct) => {
  var since = DateTimeOffset.UtcNow.AddDays(-30);
  var orders = await db.Orders.AsNoTracking().Where(x => x.CreatedAt >= since).ToListAsync(ct);
  var daily = orders.GroupBy(x => x.CreatedAt.UtcDateTime.Date)
    .OrderBy(x => x.Key).Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"),
      Orders = g.Count(), Completed = g.Count(x => x.Status == "Completed"),
      Cancelled = g.Count(x => x.Status == "Cancelled") }).ToList();
  var sales = orders.Where(x => x.Status == "Completed")
    .GroupBy(x => x.Currency).Select(g => new { Currency = g.Key, Count = g.Count(), Revenue = g.Sum(x => x.Total) }).ToList();
  return Results.Ok(new { PeriodDays = 30, GeneratedAt = DateTimeOffset.UtcNow,
    Orders = orders.Count, Pending = orders.Count(x => x.Status == "Pending"),
    Preparing = orders.Count(x => x.Status == "Preparing"),
    Ready = orders.Count(x => x.Status == "Ready"), Daily = daily, Sales = sales });
}).RequireAuthorization("AdminOnly");
// Stage 11: inventory controls. Null stock means unlimited; zero means sold out.
app.MapGet("/api/admin/inventory",async (RestaurantDb db)=>await db.MenuItems.AsNoTracking()
  .OrderBy(x=>x.StockQuantity).Select(x=>new {x.Id,x.NameEn,x.NameFa,x.Active,x.StockQuantity,
    LowStock=x.StockQuantity!=null && x.StockQuantity<=5}).ToListAsync()).RequireAuthorization("AdminOnly");
app.MapPatch("/api/admin/inventory/{id:int}",async (int id,StockInput input,HttpRequest request,RestaurantDb db)=>{
  if(!IsSameOriginWrite(request))return Results.StatusCode(403);
  if(input.StockQuantity is < 0 or > 1000000)return Results.BadRequest(new {error="Invalid stock"});
  var item=await db.MenuItems.FindAsync(id);if(item is null)return Results.NotFound();
  var previousStock=item.StockQuantity;
  item.StockQuantity=input.StockQuantity;
  db.AuditEntries.Add(new AuditEntry { ActorId=request.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown", Action="InventoryAdjusted", Target=$"menu:{id}:{previousStock?.ToString() ?? "unlimited"}->{input.StockQuantity?.ToString() ?? "unlimited"}" });
  await db.SaveChangesAsync();
  return Results.Ok(new {item.Id,item.StockQuantity});
}).RequireAuthorization("AdminOnly");
// Stage 13: read-only audit trail, administrator-only.
app.MapGet("/api/admin/audit", async (RestaurantDb db) => await db.AuditEntries.AsNoTracking()
  .OrderByDescending(x => x.Id).Take(150).ToListAsync()).RequireAuthorization("AdminOnly");
// Stage 12: administrator-managed staff accounts. No public registration endpoint.
app.MapGet("/api/admin/staff", async (UserManager<IdentityUser> users) => {
  var list = await users.Users.OrderBy(x => x.Email).Take(200).ToListAsync();
  var result = new List<object>();
  foreach (var user in list) result.Add(new { user.Id, user.Email, user.LockoutEnabled, user.LockoutEnd, Roles = await users.GetRolesAsync(user) });
  return Results.Ok(result);
}).RequireAuthorization("AdminOnly");
app.MapPost("/api/admin/staff", async (StaffCreateInput input, HttpRequest request, UserManager<IdentityUser> users, RoleManager<IdentityRole> roles) => {
  if (!IsSameOriginWrite(request)) return Results.StatusCode(403);
  if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 256 || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 12 || input.Role is not ("Manager" or "Kitchen" or "Reception")) return Results.BadRequest(new {error="Invalid staff account"});
  if (await users.FindByEmailAsync(input.Email.Trim()) is not null) return Results.Conflict(new {error="Email already registered"});
  if (!await roles.RoleExistsAsync(input.Role)) { var created = await roles.CreateAsync(new IdentityRole(input.Role)); if (!created.Succeeded) return Results.BadRequest(new {error="Unable to create role"}); }
  var user = new IdentityUser {UserName=input.Email.Trim(),Email=input.Email.Trim(),EmailConfirmed=true,LockoutEnabled=true};
  var result=await users.CreateAsync(user,input.Password);
  if(!result.Succeeded) return Results.BadRequest(new {errors=result.Errors.Select(x=>x.Description)});
  var assigned=await users.AddToRoleAsync(user,input.Role);
  if(!assigned.Succeeded) { await users.DeleteAsync(user); return Results.BadRequest(new {error="Role assignment failed"}); }
  return Results.Created($"/api/admin/staff/{user.Id}",new {user.Id,user.Email,Role=input.Role});
}).RequireAuthorization("AdminOnly");
app.MapPatch("/api/admin/staff/{id}/role", async (string id, StaffRoleInput input, HttpRequest request, System.Security.Claims.ClaimsPrincipal principal, UserManager<IdentityUser> users, RoleManager<IdentityRole> roles) => {
  if (!IsSameOriginWrite(request)) return Results.StatusCode(403);
  if (input.Role is not ("Manager" or "Kitchen" or "Reception")) return Results.BadRequest();
  var user=await users.FindByIdAsync(id); if(user is null) return Results.NotFound();
  if(user.Id==users.GetUserId(principal)) return Results.Conflict(new {error="Cannot change your own role"});
  var existing=await users.GetRolesAsync(user);
  if(existing.Contains("Admin")) return Results.Conflict(new {error="Administrator roles cannot be changed here"});
  if(!await roles.RoleExistsAsync(input.Role)) await roles.CreateAsync(new IdentityRole(input.Role));
  var removed=await users.RemoveFromRolesAsync(user,existing); if(!removed.Succeeded)return Results.BadRequest();
  var added=await users.AddToRoleAsync(user,input.Role);if(!added.Succeeded)return Results.BadRequest();
  await users.UpdateSecurityStampAsync(user);
  return Results.Ok(new {user.Id,input.Role});
}).RequireAuthorization("AdminOnly");
// Stage 15: admin-only operational export with spreadsheet-safe text fields.
app.MapGet("/api/admin/inventory-export", async (RestaurantDb db) => {
  var rows=await db.MenuItems.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();
  static string Csv(string? value) { var v=value ?? ""; if(v.Length>0 && "=+-@\t\r".Contains(v[0])) v="'"+v; return "\""+v.Replace("\"","\"\"")+"\""; }
  var csv=new System.Text.StringBuilder("Id,Name EN,Name FA,Stock,Active,USD,Toman\r\n");
  foreach(var x in rows) csv.Append(x.Id).Append(',').Append(Csv(x.NameEn)).Append(',').Append(Csv(x.NameFa)).Append(',').Append(x.StockQuantity?.ToString() ?? "Unlimited").Append(',').Append(x.Active?"true":"false").Append(',').Append(x.Usd.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(x.Toman).Append("\r\n");
  return Results.File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", "dinesphere-inventory.csv");
}).RequireAuthorization("AdminOnly");
app.MapGet("/api/health",()=>Results.Ok(new {status="ok"}));
static bool ValidMenu(MenuInput x) => !string.IsNullOrWhiteSpace(x.NameEn) && x.NameEn.Length <= 150 && !string.IsNullOrWhiteSpace(x.NameFa) && x.NameFa.Length <= 150 && x.Usd >= 0 && x.Usd <= 100000 && x.Toman >= 0 && x.Toman <= 1000000000;
static bool IsSameOriginWrite(HttpRequest request) {
  var origin=request.Headers.Origin.ToString();
  return request.Headers.ContainsKey("X-Restaurant-Admin") && !string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin,UriKind.Absolute,out var uri) && uri.Authority.Equals(request.Host.Value,StringComparison.OrdinalIgnoreCase) && uri.Scheme.Equals(request.Scheme,StringComparison.OrdinalIgnoreCase);
}

app.Run();
record StaffCreateInput(string Email,string Password,string Role);
record StaffRoleInput(string Role);
record StockInput(int? StockQuantity);
record LoginInput(string Email,string Password);
record ReservationInput(string Name,string Phone,DateOnly Date,TimeOnly Time,int Guests,string? Note);
record StatusInput(string Status);
record MenuInput(string NameEn,string NameFa,decimal Usd,long Toman,bool Active);
record ChatInput(string Message,string Language);
class Reservation { public int Id {get;set;} public string Name {get;set;}="";public string Phone {get;set;}="";public DateOnly Date {get;set;} public TimeOnly Time {get;set;} public int Guests {get;set;} public string Note {get;set;}=""; public string Status {get;set;}="Pending"; }
class MenuItem { public int Id {get;set;} public string NameEn {get;set;}="";public string NameFa {get;set;}="";public decimal Usd {get;set;}public long Toman {get;set;}public bool Active {get;set;}=true; public int? StockQuantity {get;set;} }
class RestaurantDb(DbContextOptions<RestaurantDb> options):IdentityDbContext<IdentityUser,IdentityRole,string>(options) { public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>(); public DbSet<Reservation> Reservations => Set<Reservation>();public DbSet<MenuItem> MenuItems => Set<MenuItem>(); public DbSet<RestaurantOrder> Orders => Set<RestaurantOrder>(); }


record OrderLineInput(int MenuItemId,int Quantity);
record OrderInput(string Customer,string Currency,List<OrderLineInput> Items);
class RestaurantOrder { public int Id {get;set;} public string Customer {get;set;}=""; public string Currency {get;set;}="USD"; public string Status {get;set;}="Pending"; public decimal Total {get;set;} public DateTimeOffset CreatedAt {get;set;} public List<RestaurantOrderLine> Lines {get;set;}=new(); }
class RestaurantOrderLine { public int Id {get;set;} public int RestaurantOrderId {get;set;} public int MenuItemId {get;set;} public string Name {get;set;}=""; public int Quantity {get;set;} public decimal UnitPrice {get;set;} }

class AuditEntry { public long Id { get; set; } public DateTimeOffset AtUtc { get; set; } = DateTimeOffset.UtcNow; [MaxLength(100)] public string ActorId { get; set; } = ""; [MaxLength(80)] public string Action { get; set; } = ""; [MaxLength(80)] public string Target { get; set; } = ""; }
