# Chapter 4 Reference Data — System Implementation and Testing

**What this file is:** a raw, codebase-grounded fact sheet for UComplain (Community
Incident Reporting System), organized to match your Chapter 4 template section-by-section
(Introduction → Implementation Environment → Choice of Language and Tools → Description
of Modules → System Testing → Test Cases and Results → Screenshots and User Guide →
Discussion of Findings). Paste the relevant section into a new prompt ("write section
4.X using this data, in formal academic tone, ~N words") and let Claude turn it into
prose — this file is deliberately terse/tabular, not final writing. Companion to
`docs/chapter-3-reference-data.md` (System Analysis and Design) — read that one first if
you need the *why* behind a design decision mentioned here only in passing.

**What's grounded vs. what needs you:** everything under "Evidence" is pulled directly
from the actual code, test files, and configuration — you can cite it with confidence.
Sections marked **⚠ NEEDS YOUR INPUT** describe things the codebase cannot tell us
(screenshots, your own reflective commentary, real end-user feedback) — adapt the
scaffold given, don't invent specifics about people or feedback that didn't happen.

---

## 4.1 Introduction

Purpose scaffold: this chapter describes how the design presented in Chapter Three was
implemented and tested — the environment and tools used, each module of the completed
system and how it works, the testing performed at the unit, integration, and system
level, representative test cases and their results, a guided walkthrough of the working
application, and a discussion of what was learned building it.

---

## 4.2 Implementation Environment

**Evidence:** actual deployment topology and local development requirements.

**Deployment (production) environment:**

| Component | Environment |
| --- | --- |
| Backend API | Docker container (`mcr.microsoft.com/dotnet/aspnet:9.0` runtime image, built from `mcr.microsoft.com/dotnet/sdk:9.0`) on [Render](https://render.com), a Linux-based container hosting platform |
| Admin web frontend | [Vercel](https://vercel.com), Next.js's native hosting platform (serverless/edge functions + static assets) |
| Database | Supabase-hosted PostgreSQL (managed Postgres-as-a-service) |
| File storage | Supabase Storage (private bucket, S3-compatible object storage) |
| Transactional email | Resend (HTTP API — Render's free tier has no outbound SMTP) |
| Mobile app | Expo-managed React Native — runs on iOS and Android via a development build or Expo Go, and can also target web via `react-native-web` |

**Local development environment (as configured in this repository):**

| Component | Requirement |
| --- | --- |
| Backend | .NET 9 SDK; any PostgreSQL 17 instance (local or Supabase) — Npgsql doesn't care which |
| Admin web frontend | Node.js 20+, npm |
| Mobile app | Node.js, npm, Expo CLI (`npx expo start`); a physical device with Expo Go, or an iOS Simulator / Android Emulator, for testing on-device APIs (camera, location, microphone) |
| Version control | Git |

⚠ **NEEDS YOUR INPUT**: the specific machine(s) used for development (CPU/RAM, OS
version — e.g. Windows 11) and the specific physical device(s)/emulator(s) the mobile
app was actually run and tested on (make/model, OS version) — the codebase doesn't
record this, only what the software targets.

---

## 4.3 Choice of Programming Language and Tools

**Evidence:** justification grounded in the system's own actual requirements and
constraints (a system with three clients sharing one backend, built solo/by a small team,
with a relational schema of ~25 interrelated tables and hard requirements around
auditability and dual authentication schemes).

| Choice | Alternative(s) considered | Why this one |
| --- | --- | --- |
| **C# / ASP.NET Core (.NET 9)** for the backend | Node.js/Express, Python/Django | Strong static typing catches an entire class of bugs at compile time across a codebase with 25 entity types and many cross-service dependencies; Entity Framework Core's migration tooling gives a reviewable, additive-only schema-change history (see Chapter 3 §3.6.4) that this project relied on for every one of its 10 migrations; first-class async/await and a mature dependency-injection container suit a service-layered (Domain/Application/Infrastructure/Api) architecture; official Docker images make Render deployment straightforward |
| **PostgreSQL** (via Supabase) for the database | MongoDB, MySQL/MariaDB | The domain is fundamentally relational — reports, reporters, categories, admins, verification events, and every Wave 2 addition (drafts, clarifications, notifications, deletion requests) are all connected by enforced foreign keys, and several features (e.g. the last-active-SuperAdmin safeguard, idempotent draft submission, the audit trail) depend on transactional integrity a document store doesn't give for free. Supabase specifically was chosen over a self-managed Postgres instance for its managed hosting, generous free tier, and built-in Storage product (used for media attachments and data exports) under one account |
| **Next.js (React, App Router) + TypeScript** for the admin web portal | Plain React + Vite, Angular | Server/client component split and file-based routing suit a multi-page admin dashboard; the same TypeScript language as the backend's DTOs (shared mental model, if not shared code) and as the mobile app, easing context-switching for a small team; Vercel's zero-config Next.js hosting |
| **Expo (React Native) + TypeScript** for the mobile app | Flutter/Dart, native Swift/Kotlin, bare React Native | A single TypeScript/React codebase targets iOS, Android, *and* web from one source tree, and shares language (and some mental model — the mobile API client in `src/lib/api/` mirrors the backend's own `Features/<Name>/` organization) with the admin frontend and backend DTOs, rather than introducing a fourth language (Dart) into the project. Expo's managed workflow (`expo-image-picker`, `expo-location`, `expo-audio`, `expo-secure-store`) provided the exact device-API surface this app needed (camera/gallery evidence capture, GPS for incident location, voice-note recording, secure token storage) without hand-writing native modules, at the cost of eventually needing an Expo development build (not the bare Expo Go sandbox) once those native modules are linked |
| **FluentValidation** for input validation | Data annotations, manual `if` checks | Centralizes every request's validation rules in one discoverable place per feature (`Features/<Name>/Validators/`), producing one consistent `422 validation_error` envelope across the entire API surface rather than ad hoc checks scattered through service methods |
| **BCrypt.Net-Next** for password hashing | PBKDF2, Argon2, a custom hash | An industry-standard, deliberately slow adaptive hash purpose-built for passwords (work factor 12 in this project), avoiding the mistakes of a hand-rolled scheme |
| **JWT Bearer authentication** (two independent schemes: admin and reporter) | Cookie-based sessions, a single shared token scheme | Stateless tokens support horizontal scaling with no server-side session store; **two separate schemes with separate signing secrets** were chosen specifically so a leaked admin secret (or reporter secret) can never be used to forge the other kind of session — a security property a single shared scheme with role claims cannot give |
| **Resend** (HTTP API) for transactional email | SMTP via a traditional provider (SendGrid, Mailgun, raw SMTP) | Render's free tier does not expose outbound SMTP; Resend's HTTP API works over plain HTTPS from any host |
| **NativeWind** for mobile styling | React Native `StyleSheet`, styled-components | Reuses the same Tailwind CSS utility-class vocabulary as the admin frontend's Tailwind setup, so styling knowledge transfers between the two frontends despite them being different rendering targets |

---

## 4.4 Description of Modules

**Evidence:** each module below is a real, tested slice of the system — grounded in
actual service interfaces, controllers, and (for the mobile app) route groups.

| Module | What it does | How it works |
| --- | --- | --- |
| **Authentication & Identity** | Admin login and mobile reporter registration/login, entirely separate credential systems | Admin: email+password → BCrypt verify → JWT (admin scheme) + rotating refresh token. Reporter: email+password+phone → BCrypt hash stored → 6-digit HMAC-hashed OTP emailed via Resend → verified → JWT (reporter scheme, distinct signing secret) + refresh token, with a remember-me choice controlling refresh-token lifetime |
| **Incident Reporting** | Getting a report into the system from any of three channels | WhatsApp: one inbound text message → one report, no reporter interaction beyond that message. Mobile one-shot: a single `POST` with every field. Mobile draft wizard: an incrementally-filled `ReportDraft` (category → details → location → evidence, each step a `PATCH`) submitted once complete — idempotent, so a retried submit never duplicates the report |
| **Media Attachments** | Photo/video/audio/document evidence on a report or draft | Multipart upload → magic-byte content validation (not just the declared MIME type) → per-type count/size limits → written to a private Supabase Storage bucket → metadata row only written after the storage write succeeds; access is always via a freshly-issued short-lived signed URL, never a permanent link |
| **Verification & Case Management** | Keeping unverified claims out of the operational queue, and moving a verified report through its lifecycle | Every report starts `Pending`; an admin's decision (Approve/Reject/RequestClarification/MarkDuplicate/Escalate) is the only way a report changes `VerificationStatus`; `CaseStatus` then moves through an explicit allowed-transition map (e.g. `UnderReview → Assigned → InProgress → Resolved → Closed`) enforced server-side so the UI can never request an illegal jump |
| **Tracking** | Letting a reporter see and act on their own report without contacting anyone | `ReportStatusProjection` derives a single badge/stage/progress-percentage/list-bucket from the report's `(VerificationStatus, CaseStatus)` pair; a reporter can also add follow-up information to an active report or withdraw it outright while it's still early in review |
| **Clarification Loop** | Structured back-and-forth when an admin needs more information before deciding | An admin's RequestClarification decision opens a `ClarificationRequest` with a reply deadline; the reporter's reply resolves it; a periodic background job auto-closes the report if the deadline passes unanswered, recording the closure as a system-driven (not admin-driven) event |
| **Notifications** | Telling a reporter something happened to their report without them having to check | A shared `NotifyAsync` call fires from every relevant state change (verified, assigned, work started, resolved, rejected, clarification requested, auto-closed) and writes a persisted, readable/markable-read notification row; device-token registration exists for a future push-notification integration, not yet wired to an actual push send |
| **Public Map** | Letting anyone — no account needed — see nearby verified incidents | An anonymous endpoint runs a two-pass geo query (SQL bounding-box pre-filter, then exact in-memory Haversine distance) over reports that are simultaneously verified, not withdrawn, and whose reporter has opted into public visibility — that visibility check runs live in the query itself, not from a single cached flag, and coarsens location for reporters who didn't opt into precise sharing |
| **Compliance (privacy, export, deletion, retention)** | Giving a reporter control over their own data, and enforcing an organisation-wide retention policy | Privacy settings gate public-map visibility/location precision/contact-ability and take effect on existing reports immediately, not just future ones; data export is built asynchronously by a background job into a JSON bundle in private storage; account deletion is a cancellable, grace-period-delayed request executed by another background job; a third background job independently anonymizes any reporter inactive past a configured retention window — both deletion paths share one anonymization routine so they can never drift apart, and neither ever deletes the reporter's own `IncidentReport` rows |
| **Admin Dashboard & Analytics** | Operational visibility for administrators | Metric cards with trend deltas, charts (volume, category/status/verification-outcome distribution — broken down by source channel), top hotspots, assignment workload, resolution time by category, CSV export, all server-computed for a caller-selected date range |
| **Audit Logging** | An immutable record of who did what, and when | A single shared `IAuditLogger` call, made by every mutating action across every module (including system/background-job actions, with a null actor), writes one row per action with a before/after JSON snapshot — the audit log itself is read-only and SuperAdmin-gated |
| **Settings** | Organisation-wide configuration | A singleton, get-or-create `SystemSettings` row backs notification toggles, SLA hours, the duplicate-detection window, and the reporter-data-retention window the compliance module's purge job actually enforces |

---

## 4.5 System Testing

### 4.5.1 Unit Testing

**Evidence:** xUnit test classes exercising a single component/service/pure function in
isolation, with dependencies either absent or mocked (Moq).

| Test class | What it isolates | Test count |
| --- | --- | --- |
| `AuthServiceTests` | Admin auth service logic | 13 |
| `EmailOtpServiceTests` | OTP issue/verify/expiry/attempt-limit logic | 10 |
| `ReportStatusProjectionTests` | The pure `(VerificationStatus, CaseStatus) → badge/stage/progress/bucket` mapping function — one theory data-driven over every reachable combination, one over the `Withdrawn` override, one fact asserting it never throws for any enum pair at all | 15 (1 fact + 2 theories, 14 data-driven cases between them) |
| `ReportPublicVisibilityTests` | The pure `(VerificationStatus, CaseStatus, showOnPublicMap) → bool` visibility rule, over every documented case | 12 (1 theory, 12 data-driven cases) |
| `BCryptPasswordHasherTests` | Password hashing/verification | 4 |
| `AppDbContextModelTests` | The EF Core model builds without throwing, and has the expected entity count (25) — a cheap regression guard against a broken mapping | 1 |

### 4.5.2 Integration Testing

**Evidence:** `WebApplicationFactory`-based tests that boot the real ASP.NET Core host
(with EF Core swapped for an isolated InMemory database, and email/storage swapped for
recording test doubles) and drive it through real HTTP requests end to end — the same
path a real client takes, including middleware, authentication, and validation.

| Test class | Surface covered | Test count |
| --- | --- | --- |
| `MobileAuthEndpointsTests` | Mobile registration, OTP verify/resend, login/refresh/logout, password reset, consent | 14 |
| `AuthEndpointsTests` | Admin login/refresh/logout, role policies (incl. one theory over multiple restricted-role cases) | 11 |
| `ReportsAndVerificationTests` | Admin report queue, verification decisions, status transitions, assignment | 11 |
| `ReporterAccountEndpointsTests` | Privacy, stats, profile, data export, account deletion, retention purge | 10 |
| `MobileReportsEndpointsTests` | One-shot mobile report submission and attachments | 10 |
| `NotificationEndpointsTests` | Notification fan-out from every trigger point, read/read-all, device registration | 9 |
| `MobileReportTrackingEndpointsTests` | Status filtering/counts, timeline, information addition, withdrawal | 9 |
| `PublicMapEndpointsTests` | Anonymous reachability, visibility rules (incl. the security-boundary test), coarsening, radius/category filtering | 8 |
| `MobileDraftReportsEndpointsTests` | Draft CRUD, attachment upload, idempotent submit | 8 |
| `WhatsAppWebhookTests` | Signature verification, handshake, reporter reuse, non-text-message skipping | 6 |
| `ClarificationEndpointsTests` | Thread creation, reply, ownership, the auto-close sweep | 6 |
| `CategoriesAndAuditLogTests` | Category CRUD, audit-log route/filtering, regression coverage for two real bugs found in earlier manual testing | 6 |
| `AdminSourceChannelTests` | Admin-side WhatsApp/MobileApp source-channel filtering | 2 |

**Total: 165 backend tests, all passing** (xUnit + FluentAssertions + Moq + EF Core
InMemory), run via `dotnet test` from the `backend/` directory. The admin frontend
additionally has 4 Vitest unit-test files covering pure business logic mirrored from the
backend (the case-status-transition map, SLA age calculation, status badge/label
mapping, and dashboard percent-change/duration formatting) — deliberately scoped to
logic, not component rendering, since the interactive behavior of the ~40 admin
route/dialog components is covered by the manual/Playwright sessions described below
instead of a from-scratch component-test harness.

### 4.5.3 System / User Acceptance Testing

**Evidence:** end-to-end verification against a real Postgres database in a real
browser, performed at the end of each frontend-facing phase (documented in `README.md`'s
"Project status" phase log) rather than left to a single pass at the end of the project.

- **Admin web portal**: each phase (5 through 8) was verified in a real headless-browser
  (Playwright) session against real seeded Postgres data, not just passing automated
  tests — e.g. Phase 5's dashboard session, Phase 6's full verification-decision
  round-trip with live tab-count updates, Phase 7's category creation and reporter
  detail view, and Phase 8's full-route screenshot pass (desktop + a mobile viewport on
  two representative pages) with zero console errors in the final run.
- **Real-database testing surfaced real bugs that mocked/InMemory testing had masked**
  (see §4.8) — this is itself evidence that the project's acceptance testing went beyond
  "the automated suite is green" to actually exercising the system as a user would.
- **Backend API surface**: every endpoint used by either frontend is additionally
  exercised through Swagger UI (`/swagger`, available in every environment including the
  deployed Render instance) as a manual acceptance-testing tool during development.

⚠ **NEEDS YOUR INPUT**: the mobile app has no automated end-to-end test harness in the
repository (no Detox/Maestro/Playwright-for-mobile setup in `mobile/package.json`) — if
you performed manual acceptance testing on a device or simulator, document that testing
session here yourself (what you tried, on what device, what you found). If you gathered
feedback from real or sample users trying the app, that also belongs here — the codebase
has no record of user feedback to draw from.

---

## 4.6 Test Cases and Results

**Evidence:** representative test cases drawn directly from real, currently-passing
automated tests (see §4.5.2 for the source files) — every row below reflects an actual
assertion in the codebase, not a hypothetical. Expand this table with more rows from the
same test files if your appendix needs a larger set.

| # | Module | Test case (input) | Expected output | Actual output | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | Admin Auth | Log in with a correct email/password | `200 OK`, access + refresh token issued | `200 OK`, tokens issued | Pass |
| 2 | Admin Auth | Log in with a wrong password | `401 invalid_credentials` | `401 invalid_credentials` | Pass |
| 3 | Mobile Auth | Register, then verify with a correct OTP | Account activated, session issued | Account activated, session issued | Pass |
| 4 | Mobile Auth | Register an email that's already verified | `409 business_rule_violation` | `409 business_rule_violation` | Pass |
| 5 | Verification | Approve a `Pending` report | `VerificationStatus → Verified`, `CaseStatus → UnderReview`, report now appears in `/api/admin/reports` | As expected | Pass |
| 6 | Verification | Record a decision on an already-`Verified` report | `409 business_rule_violation` (already verified) | `409 business_rule_violation` | Pass |
| 7 | Verification | Reject a report with no reason supplied | `422 validation_error` (reason required) | `422 validation_error` | Pass |
| 8 | Case Status | Change status `UnderReview → Resolved` directly (skipping `InProgress`) | `409 business_rule_violation` (illegal transition) | `409 business_rule_violation` | Pass |
| 9 | Attachments | Upload a file whose content doesn't match its declared MIME type | `409 business_rule_violation`, nothing stored | `409 business_rule_violation`, storage empty | Pass |
| 10 | Draft Wizard | Submit a draft twice (retry) | Second call returns the same report, no duplicate created | Same report returned, one report total | Pass |
| 11 | Draft Wizard | Submit a draft missing a required field | `409 business_rule_violation` | `409 business_rule_violation` | Pass |
| 12 | Tracking | Withdraw a report that is already `InProgress` | `409 business_rule_violation` (too late to withdraw) | `409 business_rule_violation` | Pass |
| 13 | Clarification | Reply to a clarification request after the report was already re-decided | `409 business_rule_violation` | `409 business_rule_violation` | Pass |
| 14 | Clarification | Auto-close sweep runs against an overdue, unanswered request | Report closed, reporter notified, resolved requests left untouched | As expected | Pass |
| 15 | Public Map | Query near a verified report whose reporter opted out of the public map, even with the cached visibility flag forced `true` | Report excluded (live re-check, not the cached flag) | Report excluded | Pass |
| 16 | Public Map | Query with a small radius that excludes a distant report | Only the nearby report returned | Only the nearby report returned | Pass |
| 17 | Notifications | Mark an already-read notification read again | `readAt` unchanged (idempotent) | `readAt` unchanged | Pass |
| 18 | Devices | Register a push token already registered to a different reporter | Token reassigned to the new reporter, no duplicate row | Reassigned, one row | Pass |
| 19 | Compliance | Request data export twice while the first is still pending | Second call returns the same request | Same request returned | Pass |
| 20 | Compliance | Cancel account deletion with no request pending | `409 business_rule_violation` | `409 business_rule_violation` | Pass |
| 21 | Compliance | Account-deletion sweep runs after the grace period elapses | Reporter's PII scrubbed, sessions revoked, their `IncidentReport` rows untouched | As expected | Pass |
| 22 | WhatsApp Webhook | Inbound webhook call with an invalid signature | `401 Unauthorized`, no report created | `401 Unauthorized` | Pass |
| 23 | Authorization | A `ReadOnlyAnalyst` attempts to assign a report | `403 Forbidden` | `403 Forbidden` | Pass |
| 24 | Authorization | Deactivate the last active `SuperAdmin` | `409 business_rule_violation` (safeguard) | `409 business_rule_violation` | Pass |

**Result summary**: 165/165 backend automated tests passing at time of writing (0
failures, 0 skipped); admin frontend `tsc --noEmit` / `eslint` / `next build` clean per
the Phase 7–8 verification log in `README.md`.

---

## 4.7 System Screenshots and User Guide

⚠ **NEEDS YOUR INPUT — this section needs real screenshots**, which this tool cannot
produce for you (it doesn't run the deployed app or a device simulator). What's provided
below is a grounded **walkthrough script**: the exact routes to visit, in order, to
capture a "golden path" screenshot set for each client. Follow it against a running
instance (`npm run dev` for the admin frontend, `npx expo start` for the mobile app,
against a locally-running or deployed backend) and drop screenshots in as you go.

**Admin web portal walkthrough** (routes from Chapter 3 §3.6.5):
1. `/login` — log in as a seeded administrator.
2. `/dashboard` — the metric cards, charts, and verification-queue snapshot.
3. `/verification` — open the Pending tab, record a decision on one report.
4. `/reports` — the queue now includes the just-approved report; open its `/reports/[id]` detail page.
5. `/analytics` — a custom date range and the CSV export button.
6. `/settings` — the organisation configuration form (SuperAdmin only).

**Mobile app walkthrough** (routes from Chapter 3 §3.6.5):
1. `(auth)/splash` → onboarding carousel → `(auth)/create-account` → `(auth)/verify-email`.
2. `(auth)/consent` — the four consent grants.
3. `(app)/(tabs)/home`.
4. `(app)/report/category` through `/review` → `/submitted` — the full report wizard.
5. `(app)/(tabs)/my-reports` → tap into the just-submitted report → its timeline.
6. `(app)/(tabs)/nearby-incidents` — the public map.
7. `(app)/(tabs)/notifications`.
8. `(app)/(tabs)/profile/privacy` — the privacy toggle that removes a report from the public map.

For each screenshot, a one- or two-sentence caption naming the screen and what it
demonstrates (e.g. "Figure 4.3: the report wizard's evidence step, showing a photo
attached via `expo-image-picker` with content-type validated server-side before
storage") is standard practice for this section — write those against your own captured
images, referencing the module descriptions in §4.4 for the underlying mechanism.

---

## 4.8 Discussion of Findings

**Evidence:** real issues found and fixed during development, documented at the time
they happened (`README.md`'s phase log, and this project's own build history) — not
reconstructed after the fact.

**Findings from real-database / real-browser testing** (Wave 1, Phase 5 and Phase 7):
testing against an isolated in-memory database and mocked dependencies had let four real
bugs through that only surfaced once the system ran against an actual PostgreSQL
instance — an entity's default-value initializer defeating EF Core's insert-detection,
the EF CLI's migration tooling bypassing the app's own dependency-injection container
(so seed data silently never ran via the command line), several `GroupBy` queries that
don't translate to SQL over an enum-as-string column, and a test-parallelization race in
the integration-test host. Each was root-caused and fixed rather than worked around, and
the fix pattern (e.g. "fetch raw values, then group client-side") was then checked for
and applied everywhere else it recurred. This is a concrete illustration of why §4.5.3's
system-level testing matters as a distinct activity from §4.5.1/4.5.2's unit/integration
testing: none of those four bugs were unit- or integration-test-shaped failures — they
only existed at the boundary with a real database or a real browser.

**A recurring implementation gotcha, caught early and then guarded against**: several
Wave 2 phases added a new boolean/enum column with a non-default C# initializer (e.g.
`IsRemembered = true`). EF Core's generated migration defaults *existing* rows to the
CLR type's true default rather than the entity's initializer, silently diverging new
rows from backfilled ones unless an explicit `HasDefaultValue(...)` matching the
entity's own default is added to the EF configuration before generating the migration —
a pattern first learned the hard way in Wave 1 and then deliberately reapplied every
subsequent time the same shape of change came up, rather than being rediscovered by
trial and error each time.

**A production deployment finding**: after this project's Wave 2 backend was deployed to
Render, the container failed to start with `IOException: The configured user limit (128)
on the number of inotify instances has been reached` — ASP.NET Core's default
configuration hot-reload watcher exhausting the container's inotify budget before the
app could even bind a port. Root-caused to `WebApplication.CreateBuilder`'s default
`appsettings.json` file-watcher, and fixed by disabling that watcher in the Dockerfile
(`DOTNET_hostBuilder__reloadConfigOnChange=false`) since a Docker deployment that gets
wholly redeployed on every configuration change has no use for hot-reloading a
config file inside a running container anyway.

⚠ **NEEDS YOUR INPUT**: this section is where your own reflective voice belongs most —
how well you feel the finished system meets the objectives you set out in Chapter One,
what was genuinely difficult (time constraints, learning three unfamiliar
frameworks/languages at once, working solo vs. as a team, scope you had to cut), and how
you'd characterize the overall outcome. The evidence above gives you concrete, truthful
material to reference (real bugs, real fixes, a real production incident and its root
cause) — use it to ground your reflection in specifics rather than generic statements,
but the judgment and reflection themselves have to be yours.

---

## Quick reference: source files if you need to go deeper

- `docs/chapter-3-reference-data.md` — System Analysis and Design reference data (read first)
- `docs/mobile-api-contract.md` — full mobile API endpoint table
- `docs/api-contract.md` — full admin API endpoint table
- `README.md` — Wave 1 phase-by-phase build history, including the four real bugs referenced in §4.8
- `backend/tests/CommunityIncidentReporting.Api.Tests/` — every test file referenced in §4.5/4.6
- `backend/Dockerfile` — the Render deployment fix referenced in §4.8
- `frontend/src/lib/utils/*.test.ts` — the four admin-frontend Vitest suites
- `mobile/src/app/` — every mobile screen (file-based routing)
