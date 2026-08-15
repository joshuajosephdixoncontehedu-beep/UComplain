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

## Wave 2 — feature completeness (in progress)

A detailed external product brief (22-screen citizen-app design) specified the full
feature set beyond auth + basic submission: a draft-based report wizard, five-stage
tracking, a clarification request/reply loop with auto-close, a public incident map,
notifications, and privacy/compliance features. Full plan:
`C:\Users\joshu\.claude\plans\use-this-project-folder-purring-jellyfish.md`. Key calls
made before starting (all confirmed with the project owner):

- **WhatsApp stays fully live** alongside the mobile channel — the brief's assumption
  that mobile replaces WhatsApp entirely does not apply here. No `Reporter`/
  `SourceChannel` renames.
- **No photo-redaction pipeline.** Attachments ship exactly as built in Wave 1.
- **Haversine-in-SQL** for the public map (Phase 7) — no PostGIS, no new package, plain
  `Latitude`/`Longitude` doubles stay as-is.
- **Proxy-through-API upload stays the pattern** for the new draft attachments too — no
  presigned-direct-upload rework.
- **`VerificationStatus`+`CaseStatus` stay as two enums**, not collapsed into the brief's
  single unified status. A new pure mapping function,
  `Application/Features/MobileReports/ReportStatusProjection.cs`, derives the brief's
  five-stage tracker/badge/progress/list-bucket model from the existing pair — this is
  the "single mapping function used by every endpoint" the brief asked for, without
  touching the tested state machine in `IncidentReportService`/`VerificationService`.
- **"Draft" is a separate entity** (`ReportDraft`, Phase 3), not an `IncidentReport`
  status — a draft doesn't have most required fields yet.

### Phase 1 — Reconciliation (complete)

- `CaseStatus.Withdrawn` added (additive enum member).
- `IncidentReport` gained `WithdrawnAt`, `WithdrawalReason`, `DuplicateOfReportId`
  (nullable self-FK), `IsPubliclyVisible` (server-computed, defaults `false`, indexed —
  will gate the Phase 7 public map).
- `IncidentCategory` gained `Slug`, `IconKey`, `ColourToken` (all nullable; `Slug` has a
  filtered unique index). The admin `POST/PATCH /api/admin/categories` endpoints accept
  them additively (optional trailing parameters) — the actual 8 category values from the
  Figma design still need to be entered via the existing admin Categories page once
  available; nothing was guessed into the migration.
- New `ReporterPrivacySetting` entity (`UsePreciseLocation`, `ShowOnPublicMap`,
  `AllowResponderContact`), get-or-create pattern like `SystemSettings`. No endpoint yet
  (wired in Phase 8) — built early because Phase 7's public map depends on it existing.
- `Reporter` gained `LanguagePreference`, `TermsAcceptedAt`, `TermsAcceptedVersion`.
- Fixed a real existing gap: mobile reporters' `MaskedContactReference` was never set at
  registration (always empty string) — admins saw nothing for a mobile reporter's masked
  contact. `ReporterAuthService` now sets it via a new `MaskEmail` helper (mirrors
  `WhatsAppWebhookService.MaskPhoneNumber`'s style), e.g. `a•••••@example.com`.
- One additive migration: `AddMobileWave2Reconciliation`.

### Phase 2 — Reporter identity gaps (complete)

- `ReporterLoginRequest` gained `RememberMe` (default `true`, backward compatible).
  `ReporterRefreshToken` gained `IsRemembered` (persisted at issuance, default `true` for
  the backfill) so the long/short session category survives a refresh-token rotation
  instead of silently upgrading a short session to a long one. New
  `Jwt__ReporterShortSessionHours` option (default 12) alongside the existing
  `Jwt__ReporterRefreshTokenDays` (30).
- `Otp__ResendCooldownSeconds` default changed `60` → `45` (still overridable) to match
  the design's `0:42` countdown example.
- New `ReporterConsent` entity + `ConsentType` enum (`Location`/`Camera`/
  `Notifications`/`DataProcessing`) — append-only (each grant is a new row; no
  update-in-place), and `POST /api/mobile/auth/consent` (reporter-authenticated, accepts
  a batch of grants in one call since the design's consent screen captures all four at
  once). No revoke endpoint yet — `RevokedAt` exists on the entity for a future phase.
- One additive migration: `AddReporterConsentAndSessionRemember`.

### Phase 3 — Catalogue and drafts (complete)

- `IncidentReport` gained `Landmark`, `TruthDeclarationAcceptedAt`, `SubmittedAt` (all
  nullable — the WhatsApp path and Wave 1's one-shot `POST /reports` never set the truth
  declaration, only the new draft-submit flow does). `CreateMobileReportRequest`/
  `MobileReportDetailDto` gained `Landmark` too, for parity between the one-shot and
  draft-based creation paths.
- `GET /api/mobile/categories` — active categories only, ordered by `DisplayOrder`, using
  Phase 1's `Slug`/`IconKey`/`ColourToken` fields.
- New `ReportDraft`/`ReportDraftAttachment` entities — a draft is a separate, low-risk
  table (not a status on `IncidentReport`), filled in incrementally via
  `PATCH /reports/drafts/{id}` (full-replace semantics, same convention as the admin
  side's `UpdateReportRequest` — the client sends the wizard's whole current state each
  time, not a partial diff). Draft attachments reuse `MediaAttachmentService`'s
  validation logic (extracted into a shared `ValidateFilesAsync` helper) via new
  `UploadToDraftAsync`/`DeleteDraftAttachmentAsync` methods, storing under
  `incident-report-drafts/{draftId}/...` instead of `incident-reports/{reportId}/...`.
- **Idempotent submit, no separate idempotency key needed.** `POST
  /reports/drafts/{id}/submit` sets `ReportDraft.SubmittedReportId` once and *keeps the
  draft row* (rather than deleting it) — a retried submit for an already-submitted draft
  just returns the original report again. This is simpler and more robust than the
  originally-planned separate `SubmissionIdempotencyKey` column: the draft's own id is
  already the natural idempotency boundary. At submit, attachments are re-parented into
  real `IncidentMediaAttachment` rows (same `StoragePath`, no re-upload) and the draft
  attachment rows are removed.
- One additive migration: `AddReportDraftsAndCategoryCatalogue`.

### Phase 4 — Tracking (complete)

- `MobileReportListItemDto` gained `StatusBadge`/`TrackerStage`/`ProgressPercent`/`Bucket`
  (all from `ReportStatusProjection`) so the mobile report-list cards can show a badge and
  progress bar without a second request per row.
- `GET /api/mobile/reports?status=` — new optional `status` query param
  (`Active`/`Resolved`/`Rejected`; omitted = every report, matching the brief's "All"
  tab). `ReportStatusProjection` is a pure C# function over two already-loaded enum
  values, not a SQL-translatable expression, so bucket filtering happens after
  materializing the caller's own reports — deliberately fine at this scale since it is
  always scoped to one reporter's own report list, never the full table.
- `GET /api/mobile/reports/counts` — `ReportCountsDto { active, resolved, rejected,
  total }` for the caller's own reports, backing the list-tab badge counts.
- `GET /api/mobile/reports/{id}/timeline` — the same status-history shape already
  embedded in `MobileReportDetailDto`, as its own endpoint, in chronological (oldest
  first) order — the embedded one on the detail DTO stays newest-first, unchanged.
- New `ReportInformationAddition` entity + `GET`/`POST /api/mobile/reports/{id}/information`
  — a reporter can attach a follow-up message (optionally referencing an
  already-uploaded attachment on the same report) to an **Active**-bucket report only.
  Deliberately does **not** accept a new file upload directly: `MediaAttachmentService`
  already locks attachment mutation once a report leaves
  `VerificationPending`/`UnderReview` (see Wave 1), and loosening that lock was out of
  scope for this phase — `AttachmentId` can only reference something uploaded earlier,
  while the report was still mutable.
- `POST /api/mobile/reports/{id}/withdraw` — sets `CaseStatus.Withdrawn` +
  `WithdrawnAt`/`WithdrawalReason` directly on the report. Allowed only from
  `VerificationPending`/`UnderReview`/`Assigned` — once an officer has moved a report to
  `InProgress` or further, a reporter can no longer unilaterally withdraw it.
  **Deliberately writes no `StatusHistory` row**: `StatusHistory.ChangedByAdminId` is a
  required FK to `AdminUser`, and every existing writer of that table is an admin
  decision/transition — a reporter withdrawal has no admin actor to attribute it to.
  Rather than making that FK nullable (a schema change touching a table every admin
  status-history read already depends on), the withdrawal is recorded via the existing
  `AuditLog` (`adminUserId: null`, the same convention every other reporter-initiated
  action in this service already uses) plus the report's own `WithdrawnAt`/
  `WithdrawalReason`/`CaseStatus` fields — all already visible on both the mobile and
  admin report views.
- One additive migration: `AddMobileReportsTrackingPhase4`.

### Phase 5 — Clarification loop (complete)

- New `ClarificationRequest` (one per `RequestClarification` verification decision — a
  report can accumulate several over its lifetime) and `ClarificationResponse` (a
  reporter's reply; several may accumulate per request for back-and-forth) entities.
- `VerificationService.DecideAsync`'s existing `RequestClarification` branch now also
  creates a `ClarificationRequest` row (`DueAt = now + Clarification:DeadlineHours`, new
  option, default 48) — `Reason` was already required by the validator for this action,
  so it becomes the reporter-facing clarification message directly, with no new field.
- `GET /api/mobile/reports/{id}/clarifications` (on the existing `ReportsController`) and
  `POST /api/mobile/clarifications/{id}/reply` (new `ClarificationsController`, `id` =
  the `ClarificationRequestId`, not the report id — a reply doesn't need the report id in
  its route). A reply only succeeds while the report's `VerificationStatus` is still
  `NeedsClarification` — this single check covers both "already re-decided by an admin"
  and "already auto-closed", since both change `VerificationStatus` away from
  `NeedsClarification`.
- New `ClarificationAutoCloseJob : BackgroundService` — **the first `BackgroundService` in
  this codebase**, a plain periodic timer (no Hangfire/Quartz), matching the brief's own
  fallback allowance and this project's minimal-dependency pattern. It delegates the
  actual sweep to `IClarificationAutoCloseService.CloseOverdueAsync` (a scoped service,
  resolved fresh per tick) so tests can invoke a sweep deterministically instead of
  waiting on the timer. Closes any report whose `ClarificationRequest` is unresolved,
  past `DueAt`, and whose `VerificationStatus` is still `NeedsClarification` (guards
  against closing a report an admin already re-decided through another path): sets
  `VerificationStatus = Rejected` (not left as `NeedsClarification` — that branch of
  `ReportStatusProjection` doesn't consult `CaseStatus`, so leaving it unchanged would
  show a stale "Needs clarification" badge on an actually-closed report),
  `CaseStatus = Closed`, `ClosedAt`, and a `ResolutionSummary` explaining the auto-close.
  Same operational caveat already flagged for Wave 2 in general: Render's free tier spins
  the process down after inactivity, so this in-process timer won't fire reliably on a
  dormant instance — noted for a future move to Render's Cron Job feature, not solved
  here.
- **Deliberately writes no `StatusHistory` row** for the auto-close transition, for the
  same reason as Phase 4's withdraw (`StatusHistory.ChangedByAdminId` is a required
  admin-only FK, and this is a system action). Unlike Phase 4, though, `VerificationEvent`
  already had first-class support for a non-human actor before this phase
  (`PerformedByAdminId` was already nullable, and `VerificationMethod.AutomatedDuplicateCheck`
  already preceded this) — so the automated transition is recorded there instead, via two
  new additive enum members: `VerificationMethod.AutomatedClarificationTimeout` and
  `VerificationDecisionResult.AutoClosed`.
- One additive migration: `AddClarificationLoopPhase5`.

### Phase 6 — Notifications (complete)

- New `Notification` entity + `NotificationType` enum (`ClarificationRequested`,
  `ReportVerified`, `AssignmentMade`, `WorkStarted`, `ReportResolved`, `ReportRejected`,
  `ReportClosedDuplicate`, `ReportAutoClosed`). New cross-cutting
  `Application.Common.Interfaces.INotificationService` — deliberately mirrors
  `IAuditLogger`'s exact shape: `NotifyAsync(...)` only adds the row to the current
  `DbContext`, it never calls `SaveChangesAsync` itself, so every notification is
  persisted in the same transaction as the state change that raised it (never orphaned by
  a later failure).
- Fan-out wired into every relevant existing transition point, per the plan:
  `VerificationService.DecideAsync` (Approve → `ReportVerified`, Reject →
  `ReportRejected`, RequestClarification → `ClarificationRequested` using the admin's own
  `Reason` as the notification body, MarkDuplicate → `ReportClosedDuplicate`; Escalate is
  deliberately silent — it's an internal abuse-review flag, not a reporter-facing event),
  `IncidentReportService.AssignAsync` (→ `AssignmentMade`) and `ChangeStatusAsync`
  (→ `InProgress` → `WorkStarted`, → `Resolved` → `ReportResolved`), and
  `ClarificationAutoCloseService` (→ `ReportAutoClosed`, Phase 5's auto-close sweep).
  `MobileReportService.WithdrawAsync` (Phase 4) deliberately gets no notification — the
  reporter already knows, they did it themselves.
- `GET /api/mobile/notifications` (page/pageSize, newest first),
  `POST /api/mobile/notifications/{id}/read` (idempotent), `POST
  /api/mobile/notifications/read-all` (returns how many were updated).
- New `DeviceToken` entity + `POST /api/mobile/devices` (register/re-register),
  `DELETE /api/mobile/devices/{id}` (revoke, idempotent). **Upsert-by-token, not
  create-or-conflict**: re-registering a token already on file reassigns it to the
  calling reporter and refreshes `LastSeenAt`/`Platform` rather than erroring — the same
  token naturally moves between accounts on a shared device or after a reinstall/relogin,
  and a unique index on `Token` is what makes "does this token already exist" a single
  lookup.
- **No actual push send** — persistence and registration only, exactly as scoped: the
  brief's own fallback allowance plus the earlier explicit call to defer push sending.
  `DeviceToken` exists so a future phase (or a follow-up outside this plan) can add actual
  APNs/FCM delivery without another migration.
- One additive migration: `AddNotificationsPhase6`.

### Phase 7 — Public map (complete)

- New pure function `Application/Features/PublicMap/ReportPublicVisibility.Compute(VerificationStatus,
  CaseStatus, showOnPublicMap)` — the single rule behind `IncidentReport.IsPubliclyVisible`
  (Phase 1's own doc comment on that property named this function in advance): visible
  only when `Verified`, not `Withdrawn`, and the reporter's `ShowOnPublicMap` allows it.
  (`Rejected`/`Duplicate` can never co-occur with `Verified` in this state machine, so
  `Withdrawn` is the only "terminal/paused state" that needs an explicit check.)
- New cross-cutting `IReportVisibilityService.RecomputeAsync(report, ct)` — same
  "mutate, don't save" pattern as `IAuditLogger`/`INotificationService`. Wired into every
  place `VerificationStatus`/`CaseStatus` changes: `VerificationService.DecideAsync`,
  `IncidentReportService.ChangeStatusAsync`/`AssignAsync`, `MobileReportService.WithdrawAsync`
  (Phase 4), and `ClarificationAutoCloseService` (Phase 5). Report *creation* is
  deliberately left alone — `IsPubliclyVisible`'s CLR/DB default (`false`) is already
  correct pre-verification, so paying for a `ReporterPrivacySetting` lookup on every
  single mobile report submission would be pure waste.
- **`IsPubliclyVisible` is a defense-in-depth/performance aid, not the sole gate.**
  `GET /api/mobile/public/incidents` (new, anonymous — the one endpoint under
  `api/mobile/...` with no reporter token at all) re-checks `VerificationStatus`,
  `CaseStatus`, and a live join against `ReporterPrivacySetting.ShowOnPublicMap` directly
  in the query, rather than trusting the cached column alone. Tested explicitly: a report
  with `IsPubliclyVisible` deliberately forced `true` by the test, whose reporter has
  `ShowOnPublicMap = false`, still never appears.
- Query shape: a cheap bounding-box pre-filter (plain `Latitude`/`Longitude` arithmetic,
  fully SQL-translatable — no trig, so it doesn't fight EF Core's SQL translation) narrows
  the candidate set, then an exact Haversine distance pass runs in memory over that
  (small, radius-bounded) set for final filtering/sorting/rounding — exactly the
  Haversine-in-SQL-avoidance approach agreed at the start of Wave 2 (no PostGIS, no new
  package).
- `radiusM` is clamped server-side to [100, 20000] meters regardless of what's requested
  (defaults to 5000); results are capped at 500, nearest first — both are DoS-shape
  bounds on an anonymous, unauthenticated endpoint, not just UX defaults.
- Coarsening: when a reporter's `UsePreciseLocation` is off, returned lat/lng are rounded
  to 2 decimal places (~1.1km) instead of the true coordinates.
- Response fields deliberately stop at category, coarse/precise lat-lng, distance, and a
  relative-age bucket (`Today`/`ThisWeek`/`ThisMonth`/`Older`, from `CreatedAt`) — never
  reporter identity, description, or exact location text.
- No new migration — `IsPubliclyVisible` and `ReporterPrivacySetting` were already built
  in Phase 1 specifically so this phase wouldn't need one.

### Phase 8 — Compliance (complete) — Wave 2 finished

- New `Api/Controllers/Mobile/MeController.cs` (`api/mobile/me`) —
  `GET`/`PUT .../privacy` (Phase 1's `ReporterPrivacySetting`, get-or-create),
  `GET .../stats` (reuses `IMobileReportService.GetMyReportCountsAsync`'s bucket counts
  rather than redefining them), `PATCH` (display name, language preference — also added
  `LanguagePreference` to `ReporterProfileDto`, a real pre-existing gap: the reporter
  could set it but never read it back), `POST`/`GET .../data-export`,
  `DELETE`/`POST .../deletion/cancel`.
- **Updating `ShowOnPublicMap` recomputes `IsPubliclyVisible` for every one of the
  caller's own existing reports immediately**, not just future ones — done directly with
  the already-known new value rather than through `IReportVisibilityService` (which would
  re-query the privacy row just updated, once per report). Verified end-to-end by a test
  that toggles it off and confirms a previously-visible report drops off
  `GET /api/mobile/public/incidents` in the same request cycle.
- **Data export**: new `DataExportRequest` entity + `IDataExportProcessorService`
  (the sweep) + `DataExportJob : BackgroundService` (mirrors `ClarificationAutoCloseJob`'s
  shape exactly). Builds one JSON document — profile, the reporter's own reports,
  attachment metadata, notifications — and uploads it to the same private Supabase
  Storage bucket incident media already uses, under `data-exports/{reporterId}/{requestId}/export.json`.
  **Deliberately stores only `StoragePath`, never a download URL** — a literal deviation
  from the plan's wording ("uploads... sets a signed DownloadUrl + ExpiresAt"), in favor
  of this codebase's own stronger, already-established convention that a signed URL is
  never persisted, only ever freshly issued per read (exactly how attachment access-urls
  already work). `GET .../data-export` issues one fresh, on every call, once the export is
  `Completed`.
- **Account deletion**: new `AccountDeletionRequest` entity. `DELETE /api/mobile/me`
  only records the request (`ScheduledForAt = now + Compliance:AccountDeletionGracePeriodDays`,
  default 14) — nothing happens to the account itself until the grace period elapses, so
  the reporter can keep using their session and call `POST .../deletion/cancel` any time
  before then. `AccountDeletionJob : BackgroundService` sweeps expired Pending requests
  and anonymizes the account.
- **Retention purge**: new `ReporterRetentionPurgeJob : BackgroundService` finally
  enforces `SystemSettings.ReporterDataRetentionMonths` (a field that already existed,
  configurable via the admin Settings page, but was never enforced anywhere before this
  phase). "Last activity" = `LastLoginAt` for a reporter who's ever logged in, else their
  most recent report's `CreatedAt`, else their own `CreatedAt` — covers WhatsApp-only
  reporters too, who never log in at all.
- **One shared anonymization routine for both paths.** New cross-cutting
  `IReporterAnonymizationService.AnonymizeAsync(reporter, ct)` — used by *both*
  `AccountDeletionProcessorService` and `ReporterRetentionPurgeService`, so the two can
  never drift apart. Idempotent (checks the new `Reporter.AnonymizedAt` field first).
  Scrubs `FullName`/`Email`/`NormalizedEmail`/`PhoneNumber`/`PasswordHash` (mobile
  identity) and `WhatsAppNumberHash`/`MaskedContactReference` (WhatsApp identity, cleared
  to `""` — their own CLR default for a reporter with no WhatsApp identity, matching the
  existing filtered-unique-index convention), sets `IsActive = false`, and revokes every
  active `ReporterRefreshToken`/`DeviceToken`. **`IncidentReport` rows are never
  touched** — kept intact for audit/legal continuity, consistent with how `IsRestricted`
  already treats reporters conceptually.
- `AuditLog` coverage on every action above (`ReporterPrivacySettingUpdated`,
  `ReporterProfileUpdated`, `ReporterDataExportRequested`,
  `ReporterAccountDeletionRequested`/`Cancelled`/`Executed`, `ReporterDataRetentionPurged`)
  — same `IAuditLogger` convention as every existing mutation, `adminUserId: null` for
  every one of these (all reporter- or system-initiated, never an admin action).
- One additive migration: `AddCompliancePhase8`.

**This completes Wave 2** — all 8 phases from the plan
(`C:\Users\joshu\.claude\plans\use-this-project-folder-purring-jellyfish.md`) are now
implemented: reconciliation, reporter identity gaps, catalogue/drafts, tracking,
clarification loop, notifications, public map, and compliance.
