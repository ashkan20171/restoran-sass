# Ashkan DineSphere — Bilingual Restaurant Operations Platform

> A full-stack restaurant platform combining a premium HTML/Sass storefront with ASP.NET Core 8, SQL Server, role-based administration, order workflows, reservations and an optional AI concierge.

## Highlights

- **International-ready:** English-first (LTR, USD) and Persian (RTL, TOMAN), with independently configured menu prices rather than assumed exchange rates.
- **Customer experience:** responsive food menu, reservation requests, cart and order submission through a server API.
- **Operations:** authenticated administrator, menu management, reservation decisions, kitchen queue and currency-separated sales summary.
- **AI concierge:** server-side OpenAI integration with optional configuration. No API secrets in client JavaScript.
- **Engineering:** ASP.NET Core Minimal APIs, Entity Framework Core, SQL Server, Identity, input validation and rate limiting.

## Architecture

`HTML + Sass + vanilla JavaScript → ASP.NET Core 8 API → EF Core → SQL Server`

The public pages are served from `server/wwwroot`. The top-level site source is mirrored there. For production, use a build pipeline to avoid duplicating source assets.

## Run locally

Requirements: .NET 8 SDK, SQL Server and `dotnet-ef` tool.

```powershell
cd server
$env:ConnectionStrings__Restaurant="Server=localhost;Database=AshkanRestaurant;Trusted_Connection=True;TrustServerCertificate=True"
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
$env:BOOTSTRAP_ADMIN_EMAIL="admin@example.com"
$env:BOOTSTRAP_ADMIN_PASSWORD="REPLACE-WITH-A-STRONG-UNIQUE-PASSWORD"
dotnet run
```

**Important:** Only run `migrations add InitialCreate` on a new database without existing migrations. Existing installations must add an incremental migration. Remove bootstrap credentials after the initial account is provisioned. Use HTTPS.

Visit the HTTPS URL shown by `dotnet run`; staff dashboard: `/staff.html`. The public live menu requires menu items created through the admin API first. Without database content the live menu is empty. The AI concierge requires the `OPENAI_API_KEY` server environment variable. Payments are not implemented.

## Security and production readiness

This is a portfolio-stage implementation, **not production certified**. Complete a security audit, verify login and CSRF protections, add automated integration tests, deploy migrations, configure HTTPS and secret management, and verify legal/privacy requirements before accepting real customer information. Some legacy demo pages and localStorage workflows remain. Payment processing and inventory tracking are not implemented.

## Recruiter notes

The project demonstrates full-stack C# development, API design, relational data modeling, multilingual UX, currency-aware business logic and incremental modernization of a legacy static website. Particularly relevant to .NET Software Engineer / Full-Stack Developer roles in European markets.

## Suggested repository

`ashkan-dinesphere-restaurant-platform`

**Description:** `Bilingual restaurant platform | ASP.NET Core 8, SQL Server, HTML/Sass, secure admin workflows, USD/Toman pricing, reservations, kitchen orders & AI concierge.`

## License

No license granted unless a LICENSE file is added.

## Stage 10: Operations Analytics

See [README_STAGE10.md](README_STAGE10.md) for the 30-day operations dashboard and completed-order revenue grouped by currency.

## Stage 11
See [README_STAGE11.md](README_STAGE11.md) for inventory management, migration, and limitations.
