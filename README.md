# Community Incident Reporting System — Admin Portal

A web-based administration portal for a WhatsApp-enabled community incident reporting
system. Community members will eventually report incidents through a WhatsApp chatbot
(not built yet — see [`docs/whatsapp-integration-plan.md`](docs/whatsapp-integration-plan.md)).
Every report must pass verification before it enters the active operational queue.
Administrators use this portal to review verified cases, manage reports, users,
administrator accounts, verification queues, analytics, and audit trails.

This repository currently implements the **Admin Portal** — the web dashboard and its
backing API — plus the database schema needed to receive WhatsApp reports later. The
chatbot itself is intentionally out of scope for now.

## Architecture

```
                 ┌─────────────────────────┐
                 │   Next.js Admin Portal   │   (frontend/)
                 │  React + TypeScript      │
                 │  shadcn/ui + Recharts    │
                 └────────────┬─────────────┘
                              │ HTTPS (JSON, JWT bearer)
                              ▼
                 ┌─────────────────────────┐
                 │  ASP.NET Core Web API    │   (backend/)
                 │  Domain / Application /  │
                 │  Infrastructure / Api    │
                 └────────────┬─────────────┘
                              │ EF Core + Npgsql
                              ▼
                 ┌─────────────────────────┐
                 │  Supabase PostgreSQL     │
                 └─────────────────────────┘
```

The frontend **never** talks to Supabase directly — it only ever calls the ASP.NET Core
Web API, which is the sole owner of the database connection. See
[`docs/architecture.md`](docs/architecture.md) for the full design and
[`docs/api-contract.md`](docs/api-contract.md) for the endpoint contract.

## Technology stack

| Layer | Technology |
| --- | --- |
| Frontend | Next.js (App Router), React, TypeScript, Tailwind CSS |
| Frontend UI | shadcn/ui, Lucide React icons, Recharts, TanStack Query |
| Backend | ASP.NET Core Web API on .NET 9 |
| Data access | Entity Framework Core + Npgsql (PostgreSQL) |
| Database | Supabase PostgreSQL |
| Auth | ASP.NET Core JWT bearer authentication, role-based authorization |
| Logging | Serilog |
| Validation | FluentValidation |
| Password hashing | BCrypt |

## Repository layout

```
community-incident-reporting-system/
  frontend/     Next.js admin portal (App Router, src/ layout)
  backend/      .NET 9 solution (Domain / Application / Infrastructure / Api)
  docs/         Architecture, API contract, WhatsApp integration plan
  docker/       Dockerfiles and docker-compose for local orchestration
```

See `frontend/README` sections below and `backend/` project files for details on each
layer.

## Prerequisites

- Node.js 20+ and npm
- .NET 9 SDK
- A Supabase project (PostgreSQL connection string) — see [Supabase setup](#supabase-setup)
- Git

## Supabase setup

1. Create a project at [supabase.com](https://supabase.com).
2. In **Project Settings → Database**, copy the connection string (use the direct
   connection, port `5432`, for this server-side EF Core workload — not the pooled
   `6543` transaction pooler, since the API holds long-lived connections).
3. Never commit the real connection string. Put it in
   `backend/src/CommunityIncidentReporting.Api/.env` (see
   [`backend/.env.example`](backend/.env.example)) or your shell environment as
   `ConnectionStrings__DefaultConnection`.

## Environment variables

- Backend: copy [`backend/.env.example`](backend/.env.example) and fill in real values.
  ASP.NET Core reads `Key__SubKey` style variables (double underscore) as nested
  configuration. In Development, a `.env` file placed next to the `.csproj` is loaded
  automatically (see `Program.cs`) — this is a local convenience only and never runs
  outside Development.
- Frontend: copy [`frontend/.env.example`](frontend/.env.example) to `frontend/.env.local`.

## Running the backend

```powershell
cd backend
dotnet restore
dotnet build CommunityIncidentReporting.sln
dotnet run --project src/CommunityIncidentReporting.Api
```

The API listens on `http://localhost:5058` (see `launchSettings.json`). Swagger UI is
available at `http://localhost:5058/swagger` in Development.

Database migrations:

```powershell
cd backend
dotnet tool install --global dotnet-ef --version 9.0.19   # once per machine
dotnet ef database update --project src/CommunityIncidentReporting.Infrastructure --startup-project src/CommunityIncidentReporting.Api
```

This applies the schema **and** seeds Development data (one SuperAdmin, one admin per
role, incident categories, fictional Sierra Leonean reporters/reports across every
status, plus their verification events, assignments, notes, and audit logs) via EF
Core's `UseSeeding`/`UseAsyncSeeding` hooks — seeding only runs once (it checks whether
`admin_users` is already populated) and only in the Development environment.

Seeding requires `SeedData__SuperAdminPassword` to be set (see `backend/.env.example`)
— it is never hardcoded. Every seeded administrator account shares that one password
for local development convenience:

| Email | Role |
| --- | --- |
| aminata.kargbo@cirs.gov.sl | SuperAdmin |
| mohamed.sesay@cirs.gov.sl | IncidentManager |
| fatmata.koroma@cirs.gov.sl | Reviewer |
| ibrahim.turay@cirs.gov.sl | ReadOnlyAnalyst |

## Running the frontend

```powershell
cd frontend
npm install
npm run dev
```

The app runs on `http://localhost:3000`.

## Tests

```powershell
cd backend
dotnet test
```

```powershell
cd frontend
npm test
```

## Local URLs

| Service | URL |
| --- | --- |
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5058 |
| Swagger UI | http://localhost:5058/swagger |

## Project status

Built in phases; see commit history for what's landed.

- **Phase 0**: project scaffolding, CORS, Swagger.
- **Phase 1**: EF Core + Npgsql schema (10 entities), initial migration, Development
  seed data, BCrypt password hashing, global exception-handling middleware
  (`{ "error": { code, message, details? } }` envelope), a global FluentValidation
  filter, and Serilog request logging.
- **Phase 2**: JWT authentication (`/api/admin/auth/login|refresh|logout|me`) with
  rotating, server-revocable refresh tokens (SHA-256 hashed, never stored raw), and
  three role-based authorization policies (`SuperAdminOnly`, `ManagerOrAbove`,
  `ReviewerOrAbove`) built on ASP.NET Core's `RequireRole`. No public registration —
  administrator accounts are only ever created by a SuperAdmin (Phase 3). Requires
  `Jwt__Secret` to be set to a random value at least 32 characters long (see
  `backend/.env.example`); the API fails fast at startup with a clear message if it
  isn't. 29 backend tests (unit + `WebApplicationFactory` integration tests against
  an isolated EF Core InMemory database).
- **Phase 3**: every endpoint in `docs/api-contract.md` — Dashboard, Reports,
  Verification queue, Users (reporters), Administrators, Categories, Analytics
  (incl. CSV export), Audit Logs, Settings. All list endpoints are paginated,
  filtered, and sorted server-side (whitelisted sort columns, no dynamic-LINQ
  dependency). Every mutation writes an `AuditLog` row via a shared `IAuditLogger`.
  The operational `/reports` queue is hard-coded to `VerificationStatus == Verified`
  — non-Verified reports are only reachable via `/verification-queue`, which never
  produces an automated "false report" verdict; every decision requires a human
  action recorded with who/when/why. Case-status transitions are validated against
  an explicit allowed-transitions map (e.g. `UnderReview` can't jump straight to
  `Resolved`). A `SystemSettings` singleton row (get-or-create, no seed migration
  needed) backs `/settings`. Business rules enforced at the service layer include
  duplicate-email rejection on administrator creation and blocking deactivation of
  the last active SuperAdmin. 40 backend tests passing.

Subsequent phases add the full admin UI.

## Future WhatsApp integration

The chatbot is **not implemented**. The database schema (`Reporter`, `IncidentReport`,
`VerificationEvent`, etc.) and `SourceChannel.WhatsApp` enum value are designed so a
future WhatsApp webhook integration can create draft reports that flow into the same
verification pipeline used by administrators today. See
[`docs/whatsapp-integration-plan.md`](docs/whatsapp-integration-plan.md) for the planned
webhook contract.
