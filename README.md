# UComplain — Admin Portal

A web-based administration portal for UComplain, a WhatsApp-enabled community incident
reporting platform. Community members will eventually report incidents through a WhatsApp chatbot
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

Copy [`backend/.env.example`](backend/.env.example) to
`backend/src/CommunityIncidentReporting.Api/.env` and fill in real local values once —
it's git-ignored and loaded automatically in Development (see `Program.cs`), so every
run after that is just:

```powershell
cd backend/src/CommunityIncidentReporting.Api
dotnet run
```

No exported environment variables needed for local dev. The API listens on
`http://localhost:5058` (`launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development`
for you); Swagger UI is at `http://localhost:5058/swagger`.

If you'd rather not create a `.env` file, the equivalent one-off is setting the same
keys as PowerShell environment variables before `dotnet run` — see
`backend/.env.example` for the full list.

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

46 tests: unit tests (BCrypt hashing, auth service) and `WebApplicationFactory`
integration tests covering login/refresh/logout token rotation, the four
role-based authorization policies, the verification-first business rule
(non-Verified reports never appear in `/reports`), every verification decision
outcome (including the required-reason validation), legal/illegal case-status
transitions, assignment, audit-log writes, and the last-active-SuperAdmin
protection.

```powershell
cd frontend
npm test
```

Vitest unit tests for the pure utility logic that isn't just glue code: the
case-status-transition map mirrored from the backend, the SLA age calculation
used in the verification queue, badge-tone/label mapping, and the dashboard's
percent-change and duration formatting.

## Local URLs

| Service | URL |
| --- | --- |
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5058 |
| Swagger UI | http://localhost:5058/swagger |

## Deployment

Backend on [Render](https://render.com) as a Docker web service; frontend on
[Vercel](https://vercel.com), Vercel's native platform for Next.js. Neither needs the
other running locally — the frontend only ever calls the backend's public URL.

### Backend (Render)

The backend has its own `backend/Dockerfile` (multi-stage: `dotnet publish` in an SDK
image, running on the slimmer ASP.NET runtime image) and a `render.yaml` Blueprint at
the repo root for one-click setup. Either:

- **Blueprint**: Render dashboard → New → Blueprint → point at this repo. Render reads
  `render.yaml` and creates the service, prompting for the `sync: false` variables
  below.
- **Manual**: Render dashboard → New → Web Service → connect this repo → **Language:
  Docker** → **Root Directory: `backend`** → **Dockerfile Path: `./Dockerfile`** →
  **Health Check Path: `/health`**.

Either way, set these environment variables on the service (never commit real values
for any of these — same rule as the local `.env`):

| Variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | The Supabase connection string (session pooler or direct — see [Supabase setup](#supabase-setup)) |
| `Jwt__Secret` | A long random value **different from the local dev one** — generate with `openssl rand -base64 64` or PowerShell's `RandomNumberGenerator` |
| `Jwt__Issuer` | `UComplain` |
| `Jwt__Audience` | `UComplain.AdminPortal` |
| `Jwt__AccessTokenMinutes` | `15` |
| `Jwt__RefreshTokenDays` | `7` |
| `Cors__AllowedOrigins__0` | The deployed Vercel frontend URL, e.g. `https://ucomplain.vercel.app` (exact match, no trailing slash) |

Render assigns the container a port via the `$PORT` environment variable at runtime —
`Program.cs` reads it and binds Kestrel there directly, so nothing extra is needed on
Render's side for that. `Program.cs` also trusts `X-Forwarded-Proto`/`X-Forwarded-For`
from Render's edge proxy, so `UseHttpsRedirection()` and audit-log IP addresses both
behave correctly behind it.

Unlike the auto-migrate hook below, **Swagger is intentionally not Development-only** —
it's served in every environment, including the deployed Render instance
(`https://<your-render-service>.onrender.com/swagger`), so the live API is
self-documenting. It only exposes interactive API documentation; every endpoint still
requires a real JWT the same as any other client, so this doesn't bypass auth or leak
secrets — just the API surface itself, which is a deliberate tradeoff worth knowing
about if the API needs to stay unlisted.

**Migrations are not applied automatically in Production** — the Development-only
auto-`Migrate()` startup hook (see [Database migrations](#running-the-backend)) is
intentionally scoped out of Production, the same way it's scoped out of Testing. Apply
new migrations to Supabase from a dev machine before or after deploying a schema
change: `dotnet ef database update --project src/CommunityIncidentReporting.Infrastructure
--startup-project src/CommunityIncidentReporting.Api`, with
`ConnectionStrings__DefaultConnection` pointed at Supabase.

Free-tier Render web services spin down after inactivity — the first request after an
idle period can take 30–60 seconds to respond while it cold-starts. Not addressed here
(e.g. with an external keep-alive ping); worth knowing about if a demo needs to be
snappy on first load.

### Frontend (Vercel)

Vercel auto-detects Next.js — no Dockerfile or `vercel.json` needed. Since this repo is
a monorepo (`frontend/` and `backend/` as siblings), the one required non-default
setting is:

- Vercel dashboard → New Project → import this repo → **Root Directory: `frontend`**.

Environment variable to set on the Vercel project:

| Variable | Value |
| --- | --- |
| `NEXT_PUBLIC_API_BASE_URL` | The deployed Render backend URL, e.g. `https://ucomplain-api.onrender.com` (no trailing slash) |

Vercel's preview deployments (per-branch/PR URLs) get a different origin than the
production URL each time. The backend's CORS only allow-lists exact origins
(`Cors__AllowedOrigins__0`, `__1`, …), so preview deployments won't be able to call the
API unless their specific origin is added too — fine for a production-only setup,
worth knowing if previews need to hit a live backend.

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
- **Phase 7**: the remaining six admin pages. `/users` and `/users/[id]` (reporter
  list/detail, filtered by search/verification status/restriction, report and
  verification history, a restrict/unrestrict action for Manager-or-above).
  `/administrators` (SuperAdmin-only CRUD with role descriptions shown inline, and
  a client-side guard that disables "Deactivate" on the last active SuperAdmin —
  the backend enforces this too, but disabling the control avoids a round trip to
  find out). `/categories` (add/edit/disable with default priority and SLA hours;
  no delete, matching the backend's soft-disable-only design). `/analytics`
  (custom date range via the same `DateRangeControl` from the dashboard, reusing
  its chart components directly since `AnalyticsResponse` shares their exact data
  shapes, plus an assignment-workload table and a response-time-by-category table,
  and a CSV export). `/audit-logs` (SuperAdmin-only, filterable, read-only, with a
  detail dialog showing the before/after JSON of any entry). `/settings`
  (organisation/notification/verification-rule/privacy fields in one form, plus a
  read-only WhatsApp-integration-enabled badge next to an editable placeholder
  note — the enabled flag itself isn't part of `UpdateSettingsRequest`, since
  actually enabling it means shipping the chatbot, not flipping a setting). Every
  SuperAdmin-only page now shows an explicit "access restricted" state for other
  roles on direct navigation, matching the pattern already used for
  `/verification` — defense in depth alongside the sidebar already hiding the
  link. CSV export required its own download path rather than a plain link: the
  endpoint needs the JWT bearer token, which only lives in memory (see
  `tokenStore.ts`), so a browser-initiated `<a href>` navigation can't carry it —
  `downloadAnalyticsCsv()` fetches with the header instead and saves the response
  as a blob.

  Real-browser testing against real seeded Postgres data (the same discipline as
  every phase since 5) found two more real bugs:
  - `AuditLogsController` had no explicit route override, so it inherited the
    default `[controller]` token and resolved to `/api/admin/AuditLogs` instead of
    the documented `/api/admin/audit-logs` — the same class of bug already fixed
    for `VerificationQueueController` in Phase 3, but missed here since nothing
    had exercised this specific endpoint until the audit-logs page existed. Fixed
    with the same explicit `[Route("api/admin/audit-logs")]` override.
  - `CategoryService.CreateAsync`/`UpdateAsync` had no duplicate-name check, unlike
    `AdministratorService`'s duplicate-email check — creating a category with a
    name already in use fell all the way through to the database's unique
    constraint and surfaced as a raw 500 with an EF/Npgsql stack trace in the
    response body (a real information-disclosure smell, not just a UX rough edge).
    Fixed by adding the same `AnyAsync` pre-check + `BusinessRuleException` pattern
    already established for administrators.

  Also fixed, from the same session, a controlled/uncontrolled warning on the
  Settings page's notification `Switch`es: their `defaultValues` weren't set until
  the settings query resolved and called `reset()`, so the switches rendered
  uncontrolled (value `undefined`) on the first paint and then became controlled —
  fixed by giving the form real boolean defaults from the start. Two
  `watch()`-returned-from-`useForm()` React Compiler warnings in the administrator
  dialogs were fixed by switching to `useWatch({ control, name })`, which the
  compiler can memoize safely. Two zod-schema type-inference errors
  (`z.coerce.number()` gives a schema whose input and output types differ, which
  `useForm<T>`'s single-generic form doesn't accept) were fixed by typing those
  forms with `useForm<Input, Context, Output>` using `z.input<>`/`z.output<>`
  instead of `z.infer<>`.

  Verified end-to-end in a real headless-browser session: every one of the six
  pages loads and renders with real seeded data, a category was created through
  its dialog, a reporter was viewed with real report/verification history, and an
  audit log entry's detail dialog opened correctly — zero console errors in the
  final run. `dotnet test` still 40/40 after the `CategoryService` change; `tsc
  --noEmit`, `eslint`, and `next build` all clean.

- **Phase 8**: testing, security/accessibility review, and documentation — the last
  phase, closing out the project.
  - **Backend tests**: added a new `CategoriesAndAuditLogTests` suite (46 tests
    total, up from 40) with two regression tests for the Phase 7 bugs (the
    audit-logs route, and category duplicate-name handling — including that the
    error response never leaks the underlying `Npgsql`/EF Core exception text)
    plus the first direct assertions that a mutation actually writes an
    `AuditLog` row and that `/audit-logs` filters by entity type. The rest of the
    suite (auth, the four role policies, the verification-first rule, every
    verification decision outcome, status transitions, assignment, the
    last-SuperAdmin protection) was already in place from earlier phases.
  - **Frontend tests**: no test tooling existed yet, so this phase added Vitest
    (`npm test`) with unit tests for the pure utility logic that carries real
    business rules — the case-status-transition map mirrored from the backend,
    the SLA age calculation, badge tone/label mapping, and the dashboard's
    percent-change/duration formatting. Deliberately scoped to logic, not
    component rendering: the app's components are thin wrappers around
    TanStack Query and react-hook-form, where the interesting behavior is
    already covered by the Playwright sessions run at the end of every phase
    since 5, and a from-scratch RTL/jsdom setup for ~40 route/dialog components
    would have cost more than it returned at this stage.
  - **Accessibility**: audited every icon-only button for `aria-label` (all
    already had one) and found one real gap — the audit-log table's rows used
    `onClick` with no keyboard equivalent, so a keyboard-only user could open
    the reports list or the reporter detail page (both use real `<Link>`s) but
    not an audit-log entry's detail dialog. Fixed with `role="button"`,
    `tabIndex={0}`, an `onKeyDown` handler for Enter/Space, a focus-visible
    ring, and a descriptive `aria-label`. Forms, dialogs, and menus already get
    labeled fields, focus trapping, and keyboard navigation for free from Base
    UI; chart colors were validated for colorblind/contrast safety back in
    Phase 5.
  - **Security review**: confirmed no `.env` files or real secrets are
    tracked by git (`.gitignore` covers both frontend and backend env files;
    the only committed connection-string-shaped strings are placeholder
    schema-only/test values already documented as such). Confirmed
    `GlobalExceptionHandler` only includes exception detail in error responses
    when `IHostEnvironment.IsProduction()` is false — Production always gets
    the generic message. The Phase 7 category-duplicate-name fix (see above)
    removed the one place where a real, reachable request could produce a
    500 with an internal stack trace attached.
  - **Docs**: added [`docs/whatsapp-integration-plan.md`](docs/whatsapp-integration-plan.md)
    — a webhook contract and data-mapping plan only, no chatbot implementation,
    covering the new endpoint, signature verification, the `Reporter`/
    `IncidentReport` field mapping, consent, idempotency, and what's explicitly
    out of scope (conversation state, media storage, outbound notifications).
  - **Final visual-consistency pass**: screenshotted every route (desktop and
    a mobile viewport on two representative pages) in a real browser against
    real seeded data — zero console errors, consistent spacing/typography/
    color usage throughout, and the "last active SuperAdmin can't be
    deactivated" rule correctly reflected as a disabled button, not just a
    server-side rejection.

## Future WhatsApp integration

The chatbot is **not implemented**. The database schema (`Reporter`, `IncidentReport`,
`VerificationEvent`, etc.) and `SourceChannel.WhatsApp` enum value are designed so a
future WhatsApp webhook integration can create draft reports that flow into the same
verification pipeline used by administrators today. See
[`docs/whatsapp-integration-plan.md`](docs/whatsapp-integration-plan.md) for the planned
webhook contract.
