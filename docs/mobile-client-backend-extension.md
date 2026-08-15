# Mobile Client Backend Extension — Design Record

Status: implemented (Phases 1–8 complete; 83/83 backend tests passing). This document is
the durable design record for the mobile-app reporting channel added to the existing
WhatsApp + admin-dashboard backend. It captures the architecture decisions made before
and during implementation, so future changes can be evaluated against the same
reasoning rather than re-deriving it from the diff. See
[`mobile-api-contract.md`](mobile-api-contract.md) for the endpoint-level contract.

## Why

The system currently has one intake channel (WhatsApp, via `WhatsAppWebhookController`)
and one authenticated actor type (`AdminUser`, via a single JWT scheme). A mobile app is
being introduced as a second, richer intake channel: reporters get real accounts
(email + password, phone number, email-OTP verified) and can attach multiple media files
to a report. Both channels must land in the same admin verification/case-management
pipeline and be visible together in the dashboard.

## Baseline (as found)

- **Layers**: `CommunityIncidentReporting.Domain` → `Application` → `Infrastructure`/`Api`
  (`backend/src/`), plus `Api.Tests` and `Application.Tests` (`backend/tests/`).
- **Auth**: single JWT Bearer scheme, `AdminUser`-only. BCrypt password hashing
  (`Infrastructure/Security/BCryptPasswordHasher.cs`). Refresh tokens hashed with SHA-256,
  stored in `RefreshToken` (FK to `AdminUser`, non-nullable). No public registration
  endpoint exists — admin accounts are created by a SuperAdmin via
  `POST /api/admin/administrators`.
- **`Reporter`** (`Domain/Entities/Reporter.cs`): WhatsApp-only today. Stores only a
  one-way HMAC hash of the WhatsApp number (`WhatsAppNumberHash`) and a masked display
  string (`MaskedContactReference`) — never a raw phone number. No email, no password, no
  mobile-auth concept.
- **`IncidentReport`**: already has a `SourceChannel` enum field, but the enum
  (`Domain/Enums/SourceChannel.cs`) currently has only `WhatsApp = 0` — its own doc
  comment says it was deliberately modeled as an enum so more channels could be added
  without a schema migration. It also has a single nullable `MediaReference` string, not a
  structured multi-attachment model.
- **Database**: EF Core + Npgsql against Supabase Postgres (`AppDbContext`, two
  migrations so far: `InitialCreate`, `AddSystemSettings`). No Supabase Storage
  SDK/REST usage anywhere in the codebase.
- **Conventions**: FluentValidation validators per feature, a global `ValidationFilter`
  returning a `{ error: { code, message, details } }` envelope, `GlobalExceptionHandler`
  mapping typed exceptions to status codes, admin routes at `api/admin/[controller]` via
  `AdminControllerBase`, DTOs as C# `record`s under `Features/<Feature>/Dtos/`.
- **Email / rate limiting**: neither exists yet anywhere in the codebase.
- **WhatsApp webhook**: `WhatsAppWebhookService.ProcessMessageAsync` already hardcodes
  `SourceChannel = SourceChannel.WhatsApp` — nothing to change there for channel
  correctness.

## Architecture decisions for this extension

1. **A second, independently-secured JWT scheme for reporters** (`"ReporterScheme"`),
   registered alongside the existing default admin scheme in `Program.cs`. It uses its own
   signing secret (`Jwt__ReporterSecret`) and audience (`Jwt__ReporterAudience`) — not just
   a different audience under the same key — so a leaked admin secret cannot be used to
   forge reporter tokens, or vice versa. `Policies.RequireReporter` / `RequireAdmin` are
   added; existing `SuperAdminOnly` / `ManagerOrAbove` / `ReviewerOrAbove` policies and the
   admin scheme are untouched.
2. **`ReporterRefreshToken` is a new table**, not a reuse of `RefreshToken` (whose FK is a
   non-nullable `AdminUserId`). The existing static `RefreshTokenGenerator` helper
   (raw-token generation + SHA-256 hash) is reused as-is since it isn't tied to `AdminUser`.
3. **`Reporter` is extended in place**, not replaced. All new fields are nullable or
   default-valued so existing WhatsApp-only rows stay valid with no backfill required. A
   `PhoneNumber` field (plain text, not hashed) is added per an explicit follow-up request
   from the project owner — unlike `WhatsAppNumberHash`, this is a phone number the
   reporter directly and knowingly provides as account contact info during mobile
   registration, analogous to `Email` rather than to the anonymized WhatsApp hash. It is
   validated for shape but not itself OTP-verified or required to be unique in this pass.
4. **OTP codes are hashed with HMAC-SHA256** (a new `Otp__HashKey` secret), matching the
   existing `WhatsAppWebhookService.HashPhoneNumber` pattern already in this codebase,
   rather than BCrypt. Brute-force resistance comes from `AttemptCount`/`MaxAttempts` and a
   short expiry window, not from hash slowness — BCrypt's cost buys little extra for a
   6-digit code that's already attempt-limited.
5. **Supabase Storage is accessed via direct REST calls** through `IHttpClientFactory`
   (`POST /storage/v1/object/{bucket}/{path}` to upload, `POST
   /storage/v1/object/sign/{bucket}/{path}` for short-lived signed URLs), not a third-party
   SDK — none exists in the project yet, and the REST surface needed is small. The service
   role key is read from backend-only configuration and never appears in any API response.
6. **Rate limiting uses the built-in `Microsoft.AspNetCore.RateLimiting` middleware**
   (framework-provided since .NET 7, no new package), partitioned by email+IP, applied only
   to the new mobile auth/OTP endpoints via named policies. Admin endpoints are unaffected.
7. **Reporter-initiated actions are audited the same way WhatsApp already is**: via the
   existing `IAuditLogger.LogAsync(adminUserId: null, ...)`, with the reporter's id carried
   in the JSON payload. No `AuditLog` schema change.
8. **All admin-facing DTO changes are additive** (new trailing fields on
   `IncidentReportListItemDto`, `IncidentReportDetailDto`, `DashboardMetricsDto`,
   `AnalyticsResponse`). Since JSON is serialized by property name, this cannot break the
   existing Vercel frontend's contract.

## Phase map

1. Analyse and plan (this document).
2. Data model: extend `Reporter`; add `EmailOtpVerification`, `ReporterRefreshToken`,
   `IncidentMediaAttachment`; extend `SourceChannel` with `MobileApp`; two additive
   migrations.
3. `IEmailService` / `ResendEmailService` (Resend HTTP API, HTML+text templates for OTP,
   password reset, welcome).
4. Mobile reporter auth API (`api/mobile/auth/...`): register, verify-email-otp,
   resend-email-otp, login, refresh, logout, me, forgot-password, verify-password-reset-otp,
   reset-password.
5. Mobile incident reporting API (`api/mobile/reports/...`): create, list (own reports
   only), get by id, multipart attachment upload/delete/access-url via Supabase Storage.
6. Admin unification: source-channel filter and field on report list/detail DTOs, admin
   signed-URL endpoint for attachments, by-source dashboard/analytics metrics.
7. Supabase Storage bucket setup docs + `.env.example` additions.
8. Tests (xUnit, mirroring the existing `Integration/`/`Services/`/`Security/` structure)
   and `docs/mobile-api-contract.md`.

Each phase is built and tested (`dotnet build` / `dotnet test`) before moving to the next;
this document is updated if a decision changes during implementation.
