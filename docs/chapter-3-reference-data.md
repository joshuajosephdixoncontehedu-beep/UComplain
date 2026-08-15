# Chapter 3 Reference Data — System Analysis and Design

**What this file is:** a raw, codebase-grounded fact sheet for UComplain (Community
Incident Reporting System), organized to match your Chapter 3 template section-by-section.
Paste the relevant section into a new prompt ("write section 3.X using this data, in
formal academic tone, ~N words") and let Claude turn it into prose — this file is
deliberately terse/tabular, not final writing.

**What's grounded vs. what needs you:** everything under "Evidence" is pulled directly
from the actual code, config, migrations, and git history — you can cite it with
confidence. Sections marked **⚠ NEEDS YOUR INPUT** describe things the codebase cannot
tell us (the real-world manual process this replaces, your institution's specific
methodology rationale, screenshots) — I've given you a reasonable generic scaffold to
adapt, not invented specifics about a real organization.

---

## 3.1 Introduction

Purpose scaffold: this chapter presents the analysis and design of UComplain, covering
the development methodology adopted, an analysis of the manual/existing incident-reporting
process and its shortcomings, the functional and non-functional requirements of the
proposed system, and its design — architecture, use case model, data flow, database
schema, and user interface.

---

## 3.2 Methodology

**Evidence:** the project was built **iteratively/incrementally**, in named, sequential
phases, each one built, tested, and verified before the next began — visible directly in
the git commit history and the README's own "Project status" log.

Documented phase sequence (frontend build-out, from `README.md`):

| Phase | Deliverable |
| --- | --- |
| 0 | Project scaffolding, CORS, Swagger |
| 1 | EF Core schema (10 entities), initial migration, seed data, BCrypt hashing, global exception handling, FluentValidation filter |
| 2 | JWT authentication, rotating refresh tokens, 3 role-based policies — 29 backend tests |
| 3 | Full admin API surface (dashboard, reports, verification, users, administrators, categories, analytics, audit logs, settings) — 40 backend tests |
| 4 | Frontend foundation — design system, app shell, typed API client, auth |
| 5 | Dashboard UI + charts — first run against a real Postgres instance, which surfaced and fixed 4 real bugs |
| 6 | Reports list/detail and verification queue UI |
| 7 | Remaining six admin pages (users, administrators, categories, analytics, audit logs, settings) |
| 8 | Testing, accessibility review, security review, documentation — 46 backend tests |

A second, separate phased track (this session) extended the backend with a mobile
reporting channel, following the same discipline: **Phase 1** plan document → **Phase 2**
data model/migration → **Phase 3** email service → **Phase 4** mobile auth API →
**Phase 5** mobile reporting + media API → **Phase 6** admin unification → **Phase 7**
storage/config docs → **Phase 8** additional tests + API contract doc — ending at **83
passing backend tests**, with a build-and-full-test-suite run gating every phase.

This is characteristic of an **Agile/iterative, incremental** methodology (each phase = a
working, independently testable increment; requirements for later phases refined by what
was learned building earlier ones — e.g., Phase 5's real-Postgres run surfaced bugs that
InMemory testing had masked, changing how later phases were verified). It also has
elements of **Object-Oriented Analysis and Design**: the backend is modeled as a set of
collaborating domain entities (`Reporter`, `IncidentReport`, `AdminUser`, etc.) behind
service interfaces, consistent with OOAD's entity-first decomposition.

⚠ **NEEDS YOUR INPUT**: your own justification for *why* this methodology was chosen
academically (cost, team size of one, requirement volatility, etc.) — the above is
descriptive evidence of what was actually done, not a justification argument.

---

## 3.3 Analysis of the Existing System ⚠ NEEDS YOUR INPUT

The codebase does not describe a prior real-world manual system — it *is* the new
system. If your project replaces a specific organization's paper/verbal/ad-hoc process,
describe that here from your own fieldwork/interviews. Generic scaffold, common to this
problem domain, to adapt:

- Incidents are currently reported **verbally, by phone call, or in person** to an
  office/authority, with no standard intake form.
- Some reports arrive informally over **WhatsApp or SMS** directly to a staff member's
  personal phone, with no system of record.
- Reports are logged (if at all) in a **paper register or a spreadsheet**, maintained
  manually by one person.
- There is **no verification step** — anyone's claim is taken at face value or
  investigated ad hoc.
- **Assignment** of a report to a responsible officer happens informally (a phone call
  or a note), with no tracking of who is handling what.
- **Status updates** (in progress, resolved) are not centrally visible; a reporter or
  supervisor must ask directly to find out.
- There is **no audit trail** of who changed what and when.
- There is **no analytics/reporting** capability — no way to see volume trends,
  category breakdowns, or response-time performance without manually tallying the
  register.

---

## 3.4 Problems of the Existing System ⚠ NEEDS YOUR INPUT (mapped to what UComplain solves)

Each problem below is one the actual system demonstrably solves — pair the generic
problem statement with the corresponding "how UComplain solves it" evidence:

| Problem (generic) | How UComplain solves it (evidence) |
| --- | --- |
| No single channel of intake | Two unified intake channels — WhatsApp webhook and a mobile app — both flow into one `IncidentReport` table, one admin portal (`SourceChannel` enum: `WhatsApp`, `MobileApp`) |
| No verification before acting on a report | Every report starts `VerificationStatus.Pending` and is held out of the operational queue until an authorized admin records a decision (`VerificationEvent`: Approve / Reject / Request Clarification / Mark Duplicate / Escalate) |
| No accountability / audit trail | Every state-changing admin action (and system action like a WhatsApp/mobile submission) writes an `AuditLog` row (actor, action, entity, before/after JSON, IP, user agent, timestamp) |
| No tracked assignment or responsibility | `ReportAssignment` records who a report is assigned to, by whom, and when; auto-transitions case status on assignment |
| No visibility into status | `StatusHistory` records every case-status transition; reporters (mobile) see a safe timeline of their own report |
| No reporting/analytics | `/dashboard` and `/analytics` endpoints: volume over time, category/status/verification-outcome distribution, top hotspots, assignment workload, resolution time by category, CSV export — now also broken down **by source channel** |
| No role separation (anyone can do anything) | 4 admin roles (`SuperAdmin`, `IncidentManager`, `Reviewer`, `ReadOnlyAnalyst`) enforced by named authorization policies on every mutating endpoint |
| No protection of reporter identity | WhatsApp numbers are never stored raw — only an HMAC hash + masked display string; mobile reporter passwords are BCrypt-hashed; OTP codes are HMAC-hashed, never stored raw |

---

## 3.5 Analysis of the Proposed System

### 3.5.1 Functional Requirements

**Evidence:** derived directly from implemented, tested API endpoints.

**Administrator (web portal)** — role shown where restricted; unmarked = any authenticated admin:
- Log in / refresh session / log out (`POST /api/admin/auth/login|refresh|logout`, `GET /me`)
- View the dashboard: metric cards, trend deltas, charts, top hotspots, priority reports, recent activity, verification-queue snapshot (`GET /api/admin/dashboard`)
- View, search, filter (category/priority/status/assigned admin/**source channel**/location/date range), and sort the verified report queue (`GET /api/admin/reports`)
- View full report detail incl. masked reporter contact, decision/status history, internal notes, assignments, audit trail, **media attachments** (`GET /api/admin/reports/{id}`)
- Update report fields — category, priority, location, description *(Reviewer/IncidentManager/SuperAdmin)*
- Assign/reassign a report to an administrator *(IncidentManager/SuperAdmin)*
- Add an internal note to a report
- Change a report's case status, validated against an explicit allowed-transition map
- Fetch a short-lived signed URL to view a report's media attachment
- Review the verification queue (5 tabs: Pending / Needs Clarification / Suspected Duplicate / Flagged Abuse / Rejected) and record a verification decision with a required reason *(Reviewer/IncidentManager/SuperAdmin)*
- View/manage reporters: list, detail (report + verification history), restrict/unrestrict *(IncidentManager/SuperAdmin for restrict)*
- Manage administrator accounts: list, create, update, deactivate — with a safeguard against deactivating the last active SuperAdmin *(SuperAdmin only)*
- Manage incident categories: create/edit/disable (soft-disable only, no delete), default priority, SLA hours *(IncidentManager/SuperAdmin)*
- View analytics for a custom date range and export as CSV
- View the audit log, filterable, with a before/after detail view *(SuperAdmin only)*
- View/update organization settings (notification, verification SLA, duplicate-detection window, retention, WhatsApp toggle placeholder) *(SuperAdmin only)*

**WhatsApp reporter (anonymous, via Meta Cloud API)**:
- Send a text message to the configured WhatsApp number → automatically creates/reuses a `Reporter` (identified by a salted hash of their number) and creates one `IncidentReport`, `Pending` verification, default category, default priority
- Receive an automatic acknowledgment reply containing the case reference

**Mobile reporter (authenticated, via the mobile API)**:
- Register with full name, email, phone number, password, and explicit consent (`POST /api/mobile/auth/register`)
- Verify email via a 6-digit OTP sent by email; account activates and a session is issued (`POST /verify-email-otp`)
- Resend the OTP (rate-limited, cooldown-gated)
- Log in / refresh / log out (only verified, active, non-restricted accounts may log in)
- Reset a forgotten password via a second OTP flow (forgot → verify → reset), never revealing whether an email is registered
- View own profile (`GET /me`)
- Submit an incident report: category, description, occurrence time, location text, optional GPS coordinates (`POST /api/mobile/reports`)
- List own reports (paginated) and view a single own report's detail + status timeline
- Upload one or more media attachments (photo/video/audio/document) to a report
- Delete an attachment (only while the report is still awaiting verification/under review)
- Fetch a short-lived signed URL to view an attachment

### 3.5.2 Non-Functional Requirements

**Evidence:** derived from actual implementation choices, not aspirational.

| Quality attribute | Evidence |
| --- | --- |
| **Security — authentication** | Two independently-keyed JWT Bearer schemes (admin vs. mobile reporter — distinct signing secret *and* audience each, so one token type can never authorize the other); BCrypt password hashing (work factor 12); rotating, server-revocable refresh tokens (SHA-256 hashed at rest, never stored raw); 6-digit OTP codes HMAC-hashed at rest, expiring, single-use, attempt-limited |
| **Security — authorization** | 4-tier role-based access control on the admin side (`SuperAdmin`/`IncidentManager`/`Reviewer`/`ReadOnlyAnalyst`) via named ASP.NET Core authorization policies; reporter endpoints scoped to the authenticated reporter's own data only (ownership-checked, 404 rather than 403 to avoid confirming another reporter's data exists) |
| **Security — abuse prevention** | Rate limiting (fixed-window, IP-partitioned) on registration/login/OTP/password-reset endpoints; restricted-reporter flagging on the WhatsApp path |
| **Security — data protection** | WhatsApp numbers never stored raw (HMAC hash only); Supabase Storage bucket is private, accessed only via the backend's service-role key and short-lived signed URLs — no permanent public media URL exists; secrets (JWT keys, API keys, OTP hash key) sourced from environment variables, never hardcoded; global exception handler suppresses stack traces outside Development |
| **Auditability** | Every state-changing admin action *and* every system-originated action (WhatsApp/mobile report submission, OTP events) writes an immutable `AuditLog` row with actor, before/after JSON, IP, and user agent |
| **Performance/Scalability** | Server-side pagination on every list endpoint (whitelisted sort columns — no dynamic-LINQ injection surface); Npgsql `EnableRetryOnFailure` for transient network resilience; stateless JWT auth (no server session state) supports horizontal scaling; Dockerized backend |
| **Availability** | Backend (Render) and frontend (Vercel) deployed and scaled independently; `/health` endpoint for uptime checks; documented free-tier cold-start behavior |
| **Usability/Accessibility** | Consistent `shadcn/ui`-based design system; Phase 8 accessibility audit added keyboard support to a non-keyboard-operable control; colorblind/normal-vision-validated chart color palette; confirmation dialogs requiring a reason for consequential admin actions |
| **Reliability/Testability** | Clean/layered architecture (Domain → Application ← Infrastructure/Api) enabling isolated testing; **83 automated backend tests** (xUnit, unit + `WebApplicationFactory` integration tests against EF Core InMemory) covering auth, RBAC, verification rules, ownership enforcement, OTP lifecycle, media validation, and scheme separation; FluentValidation centralizes input validation with a consistent `422 validation_error` envelope |
| **Data integrity** | Every schema change is an additive EF Core migration (no destructive drops); database-level unique constraints (case reference, admin email, reporter WhatsApp hash, reporter normalized email, storage path); foreign keys with explicit delete behavior (Restrict/Cascade/SetNull per relationship) |
| **Maintainability** | Consistent per-feature folder structure (`Features/<Name>/{Dtos,Validators}`); a single documented error envelope (`{ error: { code, message, details } }`) across the whole API; structured logging via Serilog |

---

## 3.6 System Design

### 3.6.1 System Architecture

**Evidence:** actual deployed topology + backend project layering.

```
                    ┌───────────────────────────┐
                    │  Next.js 16 Admin Portal   │  (frontend/, Vercel)
                    │  React 19 + TypeScript     │
                    │  shadcn/ui + Recharts      │
                    └─────────────┬─────────────┘
                                  │ HTTPS (JSON, JWT bearer — admin scheme)
                                  ▼
┌──────────────┐    ┌───────────────────────────┐    ┌─────────────────┐
│ Meta WhatsApp │───▶│   ASP.NET Core Web API     │◀──▶│  Mobile App      │
│ Cloud API     │    │   .NET 9  (backend/, Render)│   │  (not built —    │
│ (webhook)     │    │   Domain / Application /   │    │   API-only;      │
└──────────────┘    │   Infrastructure / Api      │    │   JWT bearer —   │
                    │                             │    │   reporter scheme)│
                    │  ┌───────────────────────┐  │    └─────────────────┘
                    │  │ Two JWT schemes:       │  │
                    │  │  Admin (own secret)    │  │
                    │  │  Reporter (own secret) │  │
                    │  └───────────────────────┘  │
                    └───────┬──────────┬──────────┘
                            │          │
              EF Core/Npgsql│          │ HTTP (service-role key)
                            ▼          ▼
                ┌───────────────┐  ┌─────────────────────┐
                │  Supabase     │  │  Supabase Storage     │
                │  PostgreSQL   │  │  (private bucket,     │
                │  (Supabase)   │  │   signed URLs only)   │
                └───────────────┘  └─────────────────────┘
                            │
                            ▼ HTTP (API key)
                ┌───────────────────────┐
                │  Resend (transactional │
                │  email — OTP, reset,   │
                │  welcome)               │
                └───────────────────────┘
```

**Backend internal layering** (Clean/Onion architecture, 4 .NET projects):

| Layer | Project | Depends on | Contains |
| --- | --- | --- | --- |
| Domain | `CommunityIncidentReporting.Domain` | nothing | Entities, enums — zero framework dependency |
| Application | `CommunityIncidentReporting.Application` | Domain | DTOs, service interfaces, FluentValidation validators, feature-organized (`Features/<Name>/`) |
| Infrastructure | `CommunityIncidentReporting.Infrastructure` | Application, Domain | EF Core `AppDbContext` + migrations, service implementations, JWT/BCrypt security, external integrations (WhatsApp, Resend, Supabase Storage) |
| Api | `CommunityIncidentReporting.Api` | Application, Infrastructure, Domain | Controllers, middleware, DI wiring (`Program.cs`), Swagger |

**Technology stack** (from actual package/project files):

| Concern | Technology | Version |
| --- | --- | --- |
| Frontend framework | Next.js (App Router) | 16.3.0 |
| Frontend UI | React | 19.2.8 |
| Frontend language | TypeScript | ^5 |
| Frontend UI kit | shadcn/ui (Base UI primitives), Tailwind CSS | — / ^4 |
| Frontend charts | Recharts | 3.10.1 |
| Frontend data/forms | TanStack Query, React Hook Form, Zod | 5.101.4 / 7.85.0 / 4.4.3 |
| Frontend testing | Vitest, Playwright | 4.1.10 / 1.62.1 |
| Backend framework | ASP.NET Core Web API | .NET 9 |
| ORM | Entity Framework Core + Npgsql | 9.0.19 / 9.0.4 |
| Database | PostgreSQL (Supabase-hosted) | — |
| Auth | ASP.NET Core JWT Bearer (`System.IdentityModel.Tokens.Jwt`) | 8.15.0 |
| Password hashing | BCrypt.Net-Next | 4.2.0 |
| Validation | FluentValidation | 12.1.1 |
| Logging | Serilog (console + file sinks) | 10.0.0 |
| API docs | Swashbuckle (Swagger/OpenAPI) | 9.0.6 |
| Backend testing | xUnit, FluentAssertions, Moq, EF Core InMemory | — |
| Email delivery | Resend HTTP API | — |
| Object storage | Supabase Storage (REST API, no SDK) | — |
| Backend hosting | Render (Docker) | — |
| Frontend hosting | Vercel | — |

### 3.6.2 Use Case Diagram

**Evidence:** actors and use cases derived directly from controllers/policies (see 3.5.1
for the full verb list). Actor summary for the diagram:

**Actors:**
- **SuperAdmin** — every admin use case, plus administrator management, audit logs, settings
- **IncidentManager** — reports, assignment, verification decisions, categories, reporter restriction; no administrator/settings/audit-log access
- **Reviewer** — reports (limited update), verification decisions, notes; no assignment, categories, or reporter restriction
- **ReadOnlyAnalyst** — view-only: dashboard, reports, analytics
- **WhatsApp Reporter** *(anonymous/unauthenticated actor, external to the trust boundary)* — submit report via message; receive acknowledgment
- **Mobile Reporter** *(authenticated actor)* — register, verify email, log in, submit report, manage own report's attachments, view own reports
- **Meta WhatsApp Cloud API** *(external system actor)* — webhook subscription handshake, inbound message delivery
- **Resend** *(external system actor)* — outbound transactional email delivery
- **Supabase Storage** *(external system actor)* — media object storage

Use-case relationships worth showing explicitly: `«include»` "Verify Report" includes
"Record Verification Event" and conditionally "Update Case Status"; "Assign Report"
includes "Record Status History" when the prior status was `UnderReview`; "Submit Mobile
Report" and "Submit WhatsApp Report" both `«extend»` a common "Create Incident Report"
use case that produces the same downstream verification/case-management flow regardless
of channel.

### 3.6.3 Data Flow / Sequence Diagrams

**Evidence:** each numbered flow below traces an actual code path.

**Flow 1 — WhatsApp report submission**
1. Meta sends `POST /api/webhooks/whatsapp` with the message payload.
2. `WhatsAppWebhookController` verifies `X-Hub-Signature-256` (HMAC-SHA256 over the raw body); rejects with 401 on mismatch.
3. `WhatsAppWebhookService` parses the payload; ignores non-text messages.
4. Reporter lookup by HMAC hash of the sender's number; creates a new `Reporter` if none exists.
5. Creates one `IncidentReport` (`SourceChannel = WhatsApp`, `VerificationStatus = Pending`, default category/priority — `Low`/`FlaggedAbuse` if the reporter is restricted).
6. Writes an `AuditLog` row (system action, no admin actor).
7. Sends an outbound WhatsApp acknowledgment reply (best-effort; failure doesn't fail the request).
8. Returns `200 OK` to Meta (always, even on internal skip conditions, to prevent webhook retries).

**Flow 2 — Mobile registration + email verification**
1. `POST /api/mobile/auth/register` — validates input, creates an inactive `Reporter` (or resumes a previous unverified registration for the same email), hashes the password with BCrypt.
2. Issues a 6-digit OTP (`EmailOtpService`): invalidates any prior active OTP for this email+purpose, generates a cryptographically random code, stores only its HMAC hash.
3. Sends the OTP by email via `ResendEmailService` (Resend HTTP API).
4. `POST /api/mobile/auth/verify-email-otp` — validates the code (expiry, attempt count, single-use), activates the account, sets `EmailVerifiedAt`, issues a reporter-scheme JWT access token + refresh token, sends a best-effort welcome email.

**Flow 3 — Mobile report + media attachment**
1. `POST /api/mobile/reports` — reporter ID taken from the JWT claim (never the request body); server derives `Priority` from the category's default and sets `SourceChannel = MobileApp`, `VerificationStatus = Pending`.
2. `POST /api/mobile/reports/{id}/attachments` (multipart) — validates MIME type against magic bytes (not just declared `Content-Type`), enforces per-type count/size limits, uploads to Supabase Storage, then writes the `IncidentMediaAttachment` row only after the storage write succeeds (rolls back any files already stored in the same batch on a later failure).
3. `GET /api/mobile/reports/{id}/attachments/{attachmentId}/access-url` — issues a fresh short-lived signed URL on every call.

**Flow 4 — Admin verification decision**
1. Admin authenticates (`POST /api/admin/auth/login`) and calls `GET /api/admin/verification-queue`.
2. `POST /api/admin/reports/{id}/verification-decision` with `{ action, reason }` — action ∈ Approve/Reject/RequestClarification/MarkDuplicate/Escalate.
3. Writes a `VerificationEvent` (attempt-numbered); on Approve, transitions `VerificationStatus → Verified` and `CaseStatus → UnderReview` (writing a `StatusHistory` row); writes an `AuditLog` row.
4. Approved reports become visible in the operational `GET /api/admin/reports` queue for the first time (it is hard-scoped to `VerificationStatus == Verified`).

### 3.6.4 Database Design

**Evidence:** exact entity/field/relationship list from the EF Core model (14 tables,
`snake_case` table names, enums stored as `text` for tooling compatibility). All new
tables/columns since the initial schema were added via additive migrations only.

| Table | Key fields | Relationships |
| --- | --- | --- |
| `admin_users` | Id (PK), FullName, Email (unique), PasswordHash, Role (enum), IsActive, LastLoginAt, CreatedAt, UpdatedAt | 1—* `refresh_tokens`; 1—* `incident_reports` (AssignedAdmin); 1—* `report_assignments`, `status_histories`, `internal_notes`, `verification_events`, `audit_logs` (as actor) |
| `reporters` | Id (PK), WhatsAppNumberHash (unique, filtered), MaskedContactReference, **FullName, Email (unique, filtered), NormalizedEmail, PhoneNumber, PasswordHash, EmailVerifiedAt, IsActive, RestrictionReason, LastLoginAt** *(added for mobile)*, VerificationStatus, ConsentAt, IsRestricted, CreatedAt, UpdatedAt | 1—* `incident_reports`; 1—* `verification_events`; 1—* `reporter_refresh_tokens` |
| `incident_categories` | Id (PK), Name (unique), Description, DefaultPriority (enum), SlaHours, IsActive, DisplayOrder, CreatedAt, UpdatedAt | 1—* `incident_reports` |
| `incident_reports` | Id (PK), CaseReference (unique, DB-sequence-generated e.g. `CIRS-2026-000001`), ReporterId (FK), CategoryId (FK), **SourceChannel (enum: WhatsApp/MobileApp)**, Description, IncidentOccurredAt, LocationDescription, Latitude, Longitude, MediaReference (legacy single-string field), VerificationStatus (enum), CaseStatus (enum), Priority (enum), AssignedAdminId (FK, nullable), ResolutionSummary, CreatedAt, UpdatedAt, ClosedAt | *—1 `reporters`, *—1 `incident_categories`, *—1 `admin_users` (assignee, SetNull); 1—* `verification_events`, `report_assignments`, `status_histories`, `internal_notes`, **`incident_media_attachments`** |
| `verification_events` | Id (PK), IncidentReportId (FK, cascade), ReporterId (FK), VerificationMethod (enum), Result (enum), AttemptNumber, Notes, PerformedByAdminId (FK, nullable = automated), CreatedAt | *—1 `incident_reports`, *—1 `reporters`, *—1 `admin_users` |
| `report_assignments` | Id (PK), IncidentReportId (FK, cascade), AdminUserId (FK), AssignedByAdminId (FK), AssignedAt, UnassignedAt | *—1 `incident_reports`, *—2 `admin_users` |
| `status_histories` | Id (PK), IncidentReportId (FK, cascade), PreviousStatus, NewStatus (enum), ChangedByAdminId (FK), Notes, CreatedAt | *—1 `incident_reports`, *—1 `admin_users` |
| `internal_notes` | Id (PK), IncidentReportId (FK, cascade), Content, CreatedByAdminId (FK), CreatedAt, UpdatedAt | *—1 `incident_reports`, *—1 `admin_users` |
| `audit_logs` | Id (PK), AdminUserId (FK, nullable = system action), Action, EntityType, EntityId, PreviousValueJson, NewValueJson, IpAddress, UserAgent, CreatedAt | *—1 `admin_users` (SetNull) |
| `refresh_tokens` | Id (PK), AdminUserId (FK, cascade), TokenHash (unique, SHA-256), ExpiresAt, RevokedAt, ReplacedByTokenHash, CreatedAt | *—1 `admin_users` — **admin sessions only** |
| `system_settings` | Id (PK, singleton), OrganizationName, OrganizationContactEmail, NotifyOnNewVerifiedReport, NotifyOnCriticalPriority, DefaultVerificationSlaHours, DuplicateDetectionWindowHours, ReporterDataRetentionMonths, AuditLogRetentionMonths, WhatsAppIntegrationEnabled, WhatsAppPlaceholderNote, UpdatedAt, UpdatedByAdminId | *—1 `admin_users` |
| `email_otp_verifications` *(new)* | Id (PK), ReporterId (FK, nullable, SetNull), Email, Purpose (enum: SignUpVerification/PasswordReset/EmailChange), CodeHash (HMAC), ExpiresAt, AttemptCount, MaxAttempts, IsUsed, UsedAt, CreatedAt, RequestIp, UserAgent | *—1 `reporters` (nullable) |
| `reporter_refresh_tokens` *(new)* | Id (PK), ReporterId (FK, cascade), TokenHash (unique, SHA-256), ExpiresAt, RevokedAt, ReplacedByTokenHash, CreatedAt | *—1 `reporters` — **mobile reporter sessions only, separate from admin's `refresh_tokens`** |
| `incident_media_attachments` *(new)* | Id (PK), IncidentReportId (FK, cascade), FileName, StoragePath (unique), PublicOrSignedUrlReference, MediaType (enum: Image/Video/Audio/Document), MimeType, FileSizeBytes, SortOrder, UploadedAt, UploadedByReporterId (nullable), IsDeleted, DeletedAt | *—1 `incident_reports` |

**Enumerations** (stored as `text`, not native Postgres enum types):

| Enum | Values |
| --- | --- |
| `AdminRole` | SuperAdmin, IncidentManager, Reviewer, ReadOnlyAnalyst |
| `VerificationStatus` | Pending, Verified, NeedsClarification, SuspectedDuplicate, FlaggedAbuse, Rejected |
| `CaseStatus` | VerificationPending, UnderReview, Assigned, InProgress, Resolved, Closed, Rejected, Duplicate |
| `IncidentPriority` | Low, Medium, High, Critical |
| `SourceChannel` | WhatsApp, MobileApp |
| `VerificationMethod` | AdminReview, AutomatedDuplicateCheck, ReporterClarification |
| `VerificationDecisionResult` | Approved, Rejected, ClarificationRequested, MarkedDuplicate, Escalated |
| `EmailOtpPurpose` | SignUpVerification, PasswordReset, EmailChange |
| `MediaType` | Image, Video, Audio, Document |

**Migration history** (all additive, chronological): `InitialCreate` →
`AddSystemSettings` → `AddReporterMobileAuthAndMedia`.

For an ERD, draw the tables above as boxes with the listed fields, connecting FKs as
labeled relationship lines (1–to–many everywhere in this schema; no many-to-many tables
exist).

### 3.6.5 Interface (UI/UX) Design

**Evidence:** actual routed pages in the Next.js App Router (`frontend/src/app/`).

| Route | Page |
| --- | --- |
| `/login` | Admin login (react-hook-form + zod, loading/error states, remember-me) |
| `/dashboard` | Metric cards w/ trend deltas, date-range control, Recharts charts, top hotspots, priority reports, recent activity, verification-queue snapshot |
| `/reports` | Paginated/filterable/sortable report list; URL-synced filters; role-gated bulk assignment |
| `/reports/[id]` | Full report detail, masked reporter contact, verification/status timelines, notes thread, assignment panel, audit trail |
| `/verification` | Tabbed verification queue (5 `VerificationStatus` tabs), SLA age indicator, 5-action decision menu with required-reason dialog |
| `/users` , `/users/[id]` | Reporter list/detail, restrict/unrestrict action |
| `/administrators` | SuperAdmin-only CRUD, last-active-SuperAdmin protection reflected in the UI |
| `/categories` | Add/edit/disable (no delete) |
| `/analytics` | Custom date range, charts, assignment-workload table, response-time-by-category table, CSV export |
| `/audit-logs` | SuperAdmin-only, filterable, before/after detail dialog |
| `/settings` | Organization/notification/verification-rule/privacy settings form |

**Design system evidence:** `shadcn/ui` (Base UI primitives) on a custom "public-safety"
palette (slate/off-white content area, deep navy sidebar, one muted-blue primary,
reserved green/amber/red semantic colors) applied via CSS variables; a validated
colorblind-safe categorical chart palette (a hand-picked one was replaced after failing
CVD/normal-vision separation checks); role-aware navigation (SuperAdmin-only links
hidden, and an explicit "access restricted" state shown on direct navigation as
defense-in-depth); confirmation dialogs requiring a reason for consequential actions
(status changes, verification decisions); keyboard accessibility fixes documented in
Phase 8 (audit-log rows made keyboard-operable).

⚠ **Scope note for your report**: this project is **backend-only** for the mobile
reporting channel — there is no mobile app UI to screenshot. If your Chapter 3 needs
mobile-app wireframes/mockups, those are original design work you'll need to produce
separately (informed by the mobile API contract in `docs/mobile-api-contract.md`, which
defines exactly what data every screen would need to request/submit).

---

## Quick reference: source files if you need to go deeper

- `docs/architecture.md` — original architecture write-up
- `docs/api-contract.md` — full admin API endpoint table
- `docs/mobile-api-contract.md` — full mobile API endpoint table
- `docs/mobile-client-backend-extension.md` — mobile extension design record
- `docs/whatsapp-integration-plan.md` — WhatsApp webhook contract
- `README.md` — phase-by-phase build history, tech stack, deployment
- `backend/src/CommunityIncidentReporting.Domain/Entities/` — every entity definition
- `backend/src/CommunityIncidentReporting.Infrastructure/Persistence/Migrations/` — full migration history
