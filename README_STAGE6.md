# Stage 6 — API-connected bilingual restaurant

Run from `server` with .NET 8 and SQL Server. The ASP.NET Core host serves the site from `server/wwwroot`, enabling **real pending reservation submission** and **AI chat when configured**. Opening the root HTML files directly keeps the old demo-only mode.

## Setup (PowerShell)
```powershell
cd server
$env:ConnectionStrings__Restaurant="Server=localhost;Database=AshkanRestaurant;Trusted_Connection=True;TrustServerCertificate=True"
dotnet restore
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```
Visit the HTTPS URL printed by `dotnet run`, then `/staff.html` for the read-only dashboard. There is no default admin user. Provision an Identity user securely before using staff sign-in. Set `OPENAI_API_KEY` on the server to enable AI responses. Never expose secrets in JavaScript.

Reservations submitted through the hosted site are persisted in SQL Server as **Pending**, never automatically confirmed. The static demo basket is still local-only. No live checkout, payment or confirmed booking flow exists. The AI endpoint returns unavailable without a configured key.

## Security/deployment notes
Stage 5's admin write endpoints have been disabled until anti-CSRF and role-specific authorization are implemented. Public reservation and chat endpoints have rate limits. Identity account provisioning, migrations, production HTTPS, data retention and verified venue information must be completed before launch. The server's static `wwwroot` is the source of truth for hosted pages; update it after editing root HTML/Sass.
