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
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("public", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
builder.Services.AddHttpClient("ai", c => { c.BaseAddress = new Uri("https://api.openai.com/"); c.Timeout = TimeSpan.FromSeconds(20); });
var app = builder.Build();
app.UseHttpsRedirection(); app.UseDefaultFiles(); app.UseStaticFiles(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
// Identity endpoints are intentionally NOT mapped publicly. Admin users are provisioned via the CLI tool below.
app.MapPost("/api/admin/login", async (SignInManager<IdentityUser> signin, LoginInput input) => {
  if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password)) return Results.BadRequest();
  var result = await signin.PasswordSignInAsync(input.Email, input.Password, false, lockoutOnFailure:true);
  return result.Succeeded ? Results.Ok(new { message="Signed in" }) : Results.Unauthorized();
}).RequireRateLimiting("public");
app.MapPost("/api/admin/logout", async (SignInManager<IdentityUser> signin) => { await signin.SignOutAsync(); return Results.Ok(); }).RequireAuthorization();
app.MapPost("/api/reservations", async (RestaurantDb db, ReservationInput input) => {
  if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 120 || string.IsNullOrWhiteSpace(input.Phone) || input.Phone.Length > 30 || input.Guests is < 1 or > 12 || input.Date < DateOnly.FromDateTime(DateTime.Today) || input.Date > DateOnly.FromDateTime(DateTime.Today.AddMonths(6)) || input.Note?.Length > 1000) return Results.ValidationProblem(new Dictionary<string,string[]> { ["reservation"] = ["Invalid reservation details"] });
  var r = new Reservation { Name=input.Name.Trim(), Phone=input.Phone.Trim(), Date=input.Date, Time=input.Time, Guests=input.Guests, Note=input.Note?.Trim() ?? "", Status="Pending" };
  db.Reservations.Add(r); await db.SaveChangesAsync(); return Results.Created($"/api/reservations/{r.Id}", new { r.Id, r.Status, message="Request received, not confirmed" });
}).RequireRateLimiting("public");
app.MapGet("/api/admin/reservations", async (RestaurantDb db) => await db.Reservations.AsNoTracking().OrderByDescending(r => r.Id).Take(200).ToListAsync()).RequireAuthorization();
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
app.MapGet("/api/health",()=>Results.Ok(new {status="ok"}));
app.Run();
record LoginInput(string Email,string Password);
record ReservationInput(string Name,string Phone,DateOnly Date,TimeOnly Time,int Guests,string? Note);
record StatusInput(string Status);
record MenuInput(string NameEn,string NameFa,decimal Usd,long Toman,bool Active);
record ChatInput(string Message,string Language);
class Reservation { public int Id {get;set;} public string Name {get;set;}="";public string Phone {get;set;}="";public DateOnly Date {get;set;} public TimeOnly Time {get;set;} public int Guests {get;set;} public string Note {get;set;}=""; public string Status {get;set;}="Pending"; }
class MenuItem { public int Id {get;set;} public string NameEn {get;set;}="";public string NameFa {get;set;}="";public decimal Usd {get;set;}public long Toman {get;set;}public bool Active {get;set;}=true; }
class RestaurantDb(DbContextOptions<RestaurantDb> options):IdentityDbContext<IdentityUser,IdentityRole,string>(options) { public DbSet<Reservation> Reservations => Set<Reservation>();public DbSet<MenuItem> MenuItems => Set<MenuItem>(); }
