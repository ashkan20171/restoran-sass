# Stage 5 — ASP.NET Core 8 + SQL Server API foundation

This is an **opt-in backend scaffold**, not yet wired to the existing static demo pages. The static site still runs independently, using localStorage. API endpoints do not automatically make the demo booking or cart live.

## Requirements
.NET 8 SDK, SQL Server, HTTPS, EF Core CLI (`dotnet tool install --global dotnet-ef`).

Set a SQL Server connection string via .NET user-secrets or environment variable (`ConnectionStrings__Restaurant`), never commit credentials.

```powershell
cd server
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

The first admin account must be provisioned securely by an operator using ASP.NET Core Identity UserManager (not included yet). There is deliberately no anonymous registration or default admin password. Until provisioned, admin sign-in cannot succeed.

## API
- `POST /api/reservations` anonymous, pending request only
- `GET /api/menu` public menu read
- `POST /api/admin/login`, `POST /api/admin/logout` cookie authentication
- `GET /api/admin/reservations` authenticated
- `PATCH /api/admin/reservations/{id}` authenticated status update
- `PUT /api/admin/menu/{id}` authenticated price/name update
- `POST /api/assistant` server-side OpenAI proxy; optional `OPENAI_API_KEY` and `AI_MODEL` configuration

## Production blockers
Admin account provisioning, CSRF protection for cookie-authenticated writes, stronger abuse prevention, privacy/retention controls, actual venue/menu seed data, deployment, observability, integration tests, and front-end API wiring. Do not expose this scaffold publicly without hardening. No live payment handling is implemented.
