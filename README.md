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
  — or any local PostgreSQL 17 instance for development (Npgsql doesn't care which one
  it's talking to; only the connection string changes)
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

This applies the schema. **Seeding** happens separately: `dotnet ef database update`
(and `migrations add`) always run through a design-time factory that never touches
this app's real dependency injection container, so the `UseSeeding`/`UseAsyncSeeding`
hooks configured in `AddInfrastructure` can't run there. Instead, `Program.cs` calls
`Database.Migrate()` on the real, DI-resolved `AppDbContext` once at startup — but
**only in the Development environment** — which applies any pending migrations (a
no-op if already applied) and then runs the seeding hooks. In short: run
`dotnet run --project src/CommunityIncidentReporting.Api` once with
`ASPNETCORE_ENVIRONMENT=Development` and a real connection string, and the schema and
Development seed data (one SuperAdmin, one admin per role, incident categories,
fictional Sierra Leonean reporters/reports across every status, plus their
verification events, assignments, notes, and audit logs) are both in place. Seeding
only runs once — it checks whether `admin_users` is already populated first.

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
- **Phase 4**: frontend foundation. shadcn/ui (Base UI primitives) with a custom
  public-safety palette (slate/off-white content area, deep navy sidebar, one muted
  blue primary, reserved green/amber/red semantic colors) applied entirely through
  CSS variables so every generated component picks it up automatically. App shell
  (navy sidebar with role-aware nav, top bar with search/notification placeholders
  and a profile menu), a typed fetch-based API client (`lib/api/client.ts`) with
  automatic 401 refresh-and-retry, and TanStack Query wired at the root. Auth is
  **development-grade by design**: the access token lives in memory only and the
  refresh token in local/session storage (see `lib/auth/tokenStore.ts` for the
  full tradeoff notes) — the frontend still never talks to Supabase, only to this
  API, but there's no server-side cookie for `proxy.ts` to inspect, so route
  protection (`RouteGuard`) is a client-side check instead. Login page with
  react-hook-form + zod validation, loading/error states, and a remember-me
  control. All 9 authenticated routes render (placeholder content, built out in
  Phases 5–7) behind role-aware navigation. Verified in a real headless-browser
  run: unauthenticated redirect to `/login`, client-side validation, and the
  server-error path all work with zero console errors.
- **Phase 5**: the `/dashboard` page — 8 metric cards (with trend vs. the prior
  period of equal length, linking to pre-filtered `/reports` where a filter maps
  cleanly), date-range control (Today/7 days/30 days/Custom), and Recharts charts
  (report volume, category distribution, status distribution, verification
  outcomes), a top-hotspots list, a priority-reports table, a recent-activity
  timeline, and a verification-queue snapshot. Chart colors follow a validated,
  colorblind-safe categorical palette (see the dataviz skill's palette.md) rather
  than hand-picked hex values — the hand-picked ones this page started with failed
  the CVD/normal-vision separation checks (green↔amber, amber↔red pairs were
  indistinguishable); status-meaning charts (case status, verification outcome)
  use semantic status tones instead of the categorical palette, per the "a series
  that means good/bad wears status tokens, not categorical" rule, since testing
  every status hue together as one categorical palette failed validation.
  **This phase is where the project first ran against a real Supabase-shaped
  Postgres database** (a local PostgreSQL 17 instance, with the user's explicit
  sign-off before installing it) rather than EF Core InMemory, which surfaced
  four real bugs InMemory testing had been silently masking:
  - `IncidentReport.CaseReference`'s `= string.Empty` default initializer defeated
    EF Core's "value generated on add" detection, so every seeded report got an
    empty `CaseReference` and collided on the unique index.
  - `dotnet ef database update` always runs through a design-time factory that
    bypasses this app's DI container, so the Development seeding hooks configured
    in `AddInfrastructure` never actually ran via the CLI — fixed by calling
    `Database.Migrate()` on the real DI-resolved context once at Development
    startup (see the migrations section above).
  - Several dashboard/analytics queries used `GroupBy` directly on `IQueryable`
    (grouping by a joined navigation property, or an enum-as-string converted
    column) — exactly the translation failure the Phase 3 research flagged for
    the verification queue, but three more instances slipped through elsewhere.
    Fixed by fetching the raw column values first, then grouping client-side, in
    every remaining spot.
  - `WebApplicationFactory<Program>` integration tests across different test
    classes raced on a process-wide diagnostic hook when xUnit ran them in
    parallel, intermittently failing with "the entry point exited without ever
    building an IHost." Fixed with `[assembly: CollectionBehavior
    (DisableTestParallelization = true)]` — confirmed clean across several
    consecutive runs afterward.
  A fifth bug — Base UI's `DropdownMenuLabel` requiring a `DropdownMenuGroup`
  ancestor, unlike the old Radix-based pattern — was caught the same way, via a
  real click in a real browser throwing a real console error.
- **Phase 6**: `/reports` and `/reports/[id]`, and `/verification`. The reports list
  is server-side paginated, filtered (search, category, priority, status, assigned
  admin, location, date range — synced to the URL so links like the dashboard's
  `?caseStatus=UnderReview` pre-populate the filter bar), and sortable; role-gated
  bulk assignment lets a Manager-or-above select rows and assign them to an
  administrator in one action, executed as parallel calls against the existing
  single-report endpoint (`Promise.allSettled`, reporting partial failures) rather
  than adding a bulk endpoint the API doesn't have. The detail page shows full
  report data, masked reporter contact (with a restricted-reporter warning badge),
  verification and status history timelines, an internal-notes thread, an
  assignment panel, and a read-only audit trail — its status-change buttons are
  generated from a frontend copy of the backend's exact allowed-transitions map
  (`lib/utils/caseStatusTransitions.ts`) so the UI never offers a transition the
  API would reject, and every status change and verification decision goes through
  a confirmation dialog that records an optional (for approvals) or required (for
  every other outcome) reason. The verification queue is tabbed by
  `VerificationStatus` (Pending / Needs Clarification / Suspected Duplicate /
  Flagged Abuse / Rejected) with an SLA age indicator per row (elapsed time vs. the
  report's category SLA hours) and a decision menu covering all five verification
  actions, each opening a dialog that mirrors the reports detail page's
  reason-required pattern. No backend changes were needed this phase — Phase 3's
  API surface already covered every action the UI needed. Verified end-to-end in a
  real headless-browser session against real seeded Postgres data: filtering,
  sorting, note-adding, and a full verification decision (Pending → Needs
  Clarification, with the queue tab counts updating live) all confirmed with zero
  console errors.

Subsequent phases add the full admin UI.

## Future WhatsApp integration

The chatbot is **not implemented**. The database schema (`Reporter`, `IncidentReport`,
`VerificationEvent`, etc.) and `SourceChannel.WhatsApp` enum value are designed so a
future WhatsApp webhook integration can create draft reports that flow into the same
verification pipeline used by administrators today. See
[`docs/whatsapp-integration-plan.md`](docs/whatsapp-integration-plan.md) for the planned
webhook contract.
