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

**Since this file was first generated**, the project grew a third client: a native
**mobile app** (`mobile/`, Expo/React Native) alongside the existing Next.js admin portal
and the WhatsApp channel. It consumes a large "Wave 2" backend extension — draft-based
report submission, five-stage tracking, an admin clarification loop, notifications, an
anonymous public incident map, and reporter self-service privacy/data/account
compliance tools — all additive to the system this file originally described. Every
section below reflects the current, full three-client system.

---

## 3.1 Introduction

Purpose scaffold: this chapter presents the analysis and design of UComplain, covering
the development methodology adopted, an analysis of the manual/existing incident-reporting
process and its shortcomings, the functional and non-functional requirements of the
proposed system, and its design — architecture, use case model, data flow, database
schema, and user interface — across its three clients (WhatsApp, admin web portal,
mobile app).

---

## 3.2 Methodology

**Evidence:** the project was built **iteratively/incrementally**, in named, sequential
phases, each one built, tested, and verified before the next began — visible directly in
the git commit history and the README's own "Project status" log.

Documented phase sequence — **Wave 1** (admin portal + WhatsApp + backend-only mobile
auth/submission, from `README.md`):

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

A second phased track extended the backend with a mobile reporting channel
(backend-only at the time): plan document → data model/migration → email service →
mobile auth API → mobile reporting + media API → admin unification → storage/config
docs → additional tests + API contract doc — ending at **83 passing backend tests**.

**Wave 2** — a third phased track, closing the gap between that backend-only mobile API
and a real citizen-facing product, again gated by build-and-full-test-suite runs after
every phase:

| Phase | Deliverable |
| --- | --- |
| 1 — Reconciliation | `CaseStatus.Withdrawn`; `IncidentReport.IsPubliclyVisible`/duplicate-of-report fields; category slug/icon/colour fields; `ReporterPrivacySetting` entity; fixed a real gap (mobile reporters' masked contact reference was never set) |
| 2 — Reporter identity gaps | Remember-me sessions (long vs. short refresh-token lifetimes); granular, versioned, append-only consent records (`ReporterConsent`) |
| 3 — Catalogue and drafts | `GET /categories`; a full draft-based report wizard (`ReportDraft`/`ReportDraftAttachment`) with idempotent submit and attachment re-parenting |
| 4 — Tracking | Status-bucket filtering and badges (`ReportStatusProjection`), report counts, a chronological timeline endpoint, reporter-added follow-up information, reporter-initiated withdrawal |
| 5 — Clarification loop | `ClarificationRequest`/`ClarificationResponse`, wired into the existing verification-decision flow; a `BackgroundService` that auto-closes reports whose clarification request goes unanswered past a deadline |
| 6 — Notifications | Persisted, reporter-facing notifications fanned out from every relevant state change; device-token registration (no push send implemented yet — persistence/registration only) |
| 7 — Public map | An anonymous, unauthenticated endpoint returning nearby verified incidents, computed with a two-pass bounding-box-then-Haversine geo query, respecting a per-reporter public-visibility privacy setting as a live, re-checked security boundary (not just a cached flag) |
| 8 — Compliance | Reporter self-service privacy controls, account stats, profile editing, a data-export pipeline (background-built JSON bundle, private storage, freshly-signed download links), and account deletion with a cancellable grace period, backed by a shared anonymization routine also used by a data-retention-purge job |

Ending state: **165 passing backend tests**, **25 EF Core entity types**, **10
migrations** (all additive — no destructive schema change across the whole project), and
a functioning **mobile app client** (Expo/React Native, `mobile/`) that consumes this
entire surface.

This is characteristic of an **Agile/iterative, incremental** methodology (each phase = a
working, independently testable increment; requirements for later phases refined by what
was learned building earlier ones — e.g., Phase 5's real-Postgres run surfaced bugs that
InMemory testing had masked, changing how later phases were verified; Wave 2's own phase
order was itself adjusted mid-plan, moving `ReporterPrivacySetting` two phases earlier
than first planned once Phase 7's public map turned out to depend on it). It also has
elements of **Object-Oriented Analysis and Design**: the backend is modeled as a set of
collaborating domain entities (`Reporter`, `IncidentReport`, `AdminUser`, `ClarificationRequest`,
etc.) behind service interfaces, consistent with OOAD's entity-first decomposition.

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
- A reporter has **no way to see nearby incidents**, follow up on their own report
  without calling someone, or control how their information is used.

---

## 3.4 Problems of the Existing System ⚠ NEEDS YOUR INPUT (mapped to what UComplain solves)

Each problem below is one the actual system demonstrably solves — pair the generic
problem statement with the corresponding "how UComplain solves it" evidence:

| Problem (generic) | How UComplain solves it (evidence) |
| --- | --- |
| No single channel of intake | Two unified intake channels — WhatsApp webhook and a mobile app — both flow into one `IncidentReport` table, one admin portal (`SourceChannel` enum: `WhatsApp`, `MobileApp`) |
| No verification before acting on a report | Every report starts `VerificationStatus.Pending` and is held out of the operational queue until an authorized admin records a decision (`VerificationEvent`: Approve / Reject / Request Clarification / Mark Duplicate / Escalate) |
| No accountability / audit trail | Every state-changing admin action (and system action like a WhatsApp/mobile submission, an auto-close, a retention purge) writes an `AuditLog` row (actor, action, entity, before/after JSON, IP, user agent, timestamp) |
| No tracked assignment or responsibility | `ReportAssignment` records who a report is assigned to, by whom, and when; auto-transitions case status on assignment |
| No visibility into status | `StatusHistory` records every case-status transition; the mobile app's five-stage tracker (`ReportStatusProjection`) shows a reporter their own report's badge, stage, and progress without asking anyone |
| A reporter can't follow up or ask questions | `ReportInformationAddition` lets a reporter add supplementary information to their own active report; `ClarificationRequest`/`ClarificationResponse` give admins and reporters a structured back-and-forth thread, with an automatic deadline-based close if a reporter never answers |
| A reporter can't see what's happening nearby | The public map endpoint (`GET /api/mobile/public/incidents`) returns verified, publicly-visible reports near a point, radius- and category-filterable — coarsened or precise per the reporter's own privacy choice |
| A reporter has no control over or copy of their own data | Self-service privacy settings (public-map visibility, location precision, contact-ability), a one-click data export, and account deletion with a cancellable grace period |
| No reporting/analytics | `/dashboard` and `/analytics` endpoints: volume over time, category/status/verification-outcome distribution, top hotspots, assignment workload, resolution time by category, CSV export — broken down **by source channel** |
| No role separation (anyone can do anything) | 4 admin roles (`SuperAdmin`, `IncidentManager`, `Reviewer`, `ReadOnlyAnalyst`) enforced by named authorization policies on every mutating endpoint |
| No protection of reporter identity | WhatsApp numbers are never stored raw — only an HMAC hash + masked display string; mobile reporter passwords are BCrypt-hashed; OTP codes are HMAC-hashed, never stored raw; account deletion/retention-purge scrub PII while keeping reports for audit continuity |
| Old accounts/data linger forever | A background retention-purge job enforces a configurable reporter-data-retention window (`SystemSettings.ReporterDataRetentionMonths`), anonymizing inactive accounts automatically |

---

## 3.5 Analysis of the Proposed System

### 3.5.1 Functional Requirements

**Evidence:** derived directly from implemented, tested API endpoints across all three
clients.

**Administrator (web portal)** — role shown where restricted; unmarked = any authenticated admin:
- Log in / refresh session / log out (`POST /api/admin/auth/login|refresh|logout`, `GET /me`)
- View the dashboard: metric cards, trend deltas, charts, top hotspots, priority reports, recent activity, verification-queue snapshot (`GET /api/admin/dashboard`)
- View, search, filter (category/priority/status/assigned admin/source channel/location/date range), and sort the verified report queue (`GET /api/admin/reports`)
- View full report detail incl. masked reporter contact, decision/status history, internal notes, assignments, audit trail, media attachments (`GET /api/admin/reports/{id}`)
- Update report fields — category, priority, location, description *(Reviewer/IncidentManager/SuperAdmin)*
- Assign/reassign a report to an administrator *(IncidentManager/SuperAdmin)*
- Add an internal note to a report
- Change a report's case status, validated against an explicit allowed-transition map
- Fetch a short-lived signed URL to view a report's media attachment
- Review the verification queue (5 tabs: Pending / Needs Clarification / Suspected Duplicate / Flagged Abuse / Rejected) and record a verification decision with a required reason *(Reviewer/IncidentManager/SuperAdmin)* — a Request Clarification decision automatically opens a `ClarificationRequest` thread with the reporter
- View/manage reporters: list, detail (report + verification history), restrict/unrestrict *(IncidentManager/SuperAdmin for restrict)*
- Manage administrator accounts: list, create, update, deactivate — with a safeguard against deactivating the last active SuperAdmin *(SuperAdmin only)*
- Manage incident categories: create/edit/disable (soft-disable only, no delete), default priority, SLA hours, display slug/icon/colour *(IncidentManager/SuperAdmin)*
- View analytics for a custom date range and export as CSV
- View the audit log, filterable, with a before/after detail view *(SuperAdmin only)*
- View/update organization settings (notification, verification SLA, duplicate-detection window, retention, WhatsApp toggle placeholder) *(SuperAdmin only)*

**WhatsApp reporter (anonymous, via Meta Cloud API)**:
- Send a text message to the configured WhatsApp number → automatically creates/reuses a `Reporter` (identified by a salted hash of their number) and creates one `IncidentReport`, `Pending` verification, default category, default priority
- Receive an automatic acknowledgment reply containing the case reference

**Mobile reporter (authenticated, via the mobile API and app)**:

*Account:*
- Register with full name, email, phone number, password, and explicit consent; verify email via a 6-digit OTP; resend the OTP (rate-limited); log in with a remember-me choice (long vs. short session) / refresh / log out
- Reset a forgotten password via a second OTP flow, never revealing whether an email is registered
- Record granular, versioned consent grants (location, camera, notifications, data processing) — append-only, so consent history stays fully auditable
- View own profile and stats (own report counts by status bucket, member-since date); edit display name and language preference
- View and update privacy settings: public-map visibility, precise-vs-coarse location sharing, whether responders may contact them directly
- Request a downloadable export of their own data (profile, reports, attachment metadata, notifications) and download it once ready
- Request account deletion (cancellable within a grace period) or cancel a pending request

*Reporting:*
- Browse the active incident category catalogue
- Fill out a multi-step report draft (category → description/date → location → evidence) that persists between steps and can be resumed, then submit it — submission is idempotent (retrying a submit never creates a duplicate report)
- Submit a report in one call directly (non-draft path, still supported)
- Upload/delete photo, video, audio, or document evidence to a report or in-progress draft, each validated by actual file content, not just its declared type
- Fetch a short-lived signed URL to view an attachment

*Tracking:*
- List own reports, filterable by status bucket (active/resolved/rejected), with a badge/stage/progress indicator per report; view report counts per bucket
- View a single own report's full detail and a chronological status timeline
- Add supplementary information to an active report
- View and reply to a clarification request thread on a report
- Withdraw a report, while it's still early enough in the review process to do so

*Discovery and notifications:*
- View nearby verified incidents on an anonymous public map, filterable by radius and category
- View a notification inbox; mark one or all notifications read
- Register/de-register a push-notification device token

### 3.5.2 Non-Functional Requirements

**Evidence:** derived from actual implementation choices, not aspirational.

| Quality attribute | Evidence |
| --- | --- |
| **Security — authentication** | Two independently-keyed JWT Bearer schemes (admin vs. mobile reporter — distinct signing secret *and* audience each, so one token type can never authorize the other); BCrypt password hashing (work factor 12); rotating, server-revocable refresh tokens (SHA-256 hashed at rest, never stored raw); 6-digit OTP codes HMAC-hashed at rest, expiring, single-use, attempt-limited |
| **Security — authorization** | 4-tier role-based access control on the admin side (`SuperAdmin`/`IncidentManager`/`Reviewer`/`ReadOnlyAnalyst`) via named ASP.NET Core authorization policies; reporter endpoints scoped to the authenticated reporter's own data only (ownership-checked, 404 rather than 403 to avoid confirming another reporter's data exists) |
| **Security — abuse prevention** | Rate limiting (fixed-window, IP-partitioned) on registration/login/OTP/password-reset endpoints; restricted-reporter flagging on the WhatsApp path |
| **Security — data protection** | WhatsApp numbers never stored raw (HMAC hash only); Supabase Storage bucket is private, accessed only via the backend's service-role key and short-lived signed URLs — no permanent public media or export-download URL exists; secrets (JWT keys, API keys, OTP hash key) sourced from environment variables, never hardcoded; global exception handler suppresses stack traces outside Development |
| **Privacy by design** | The public map query re-checks verification status, case status, and the reporter's own visibility preference *live, in the query itself* rather than trusting a single cached flag — verified by a test that deliberately mis-sets the cached flag and confirms the report still never appears; location is coarsened to ~1.1km precision when a reporter opts out of precise sharing; account deletion and data retention share one anonymization routine so the two paths can never drift apart |
| **Auditability** | Every state-changing admin action *and* every system-originated action (WhatsApp/mobile report submission, OTP events, auto-close, retention purge) writes an immutable `AuditLog` row with actor, before/after JSON, IP, and user agent |
| **Performance/Scalability** | Server-side pagination on every list endpoint (whitelisted sort columns — no dynamic-LINQ injection surface); the public map's geo query does a cheap SQL-translatable bounding-box pre-filter before an exact in-memory Haversine pass, avoiding both a full-table scan and fighting EF Core's SQL translation of trigonometric functions; Npgsql `EnableRetryOnFailure` for transient network resilience; stateless JWT auth (no server session state) supports horizontal scaling; Dockerized backend |
| **Availability** | Backend (Render) and frontend (Vercel) deployed and scaled independently; `/health` endpoint for uptime checks; documented free-tier cold-start behavior; three periodic `BackgroundService` sweeps (clarification auto-close, data export, account deletion, retention purge) run in-process, with the free-tier cold-start/sleep tradeoff explicitly documented rather than silently assumed away |
| **Usability/Accessibility** | Consistent `shadcn/ui`-based design system on the web portal; Phase 8 accessibility audit added keyboard support to a non-keyboard-operable control; colorblind/normal-vision-validated chart color palette; confirmation dialogs requiring a reason for consequential admin actions; the mobile app's multi-step report wizard preserves progress between steps rather than losing it |
| **Reliability/Testability** | Clean/layered architecture (Domain → Application ← Infrastructure/Api) enabling isolated testing; **165 automated backend tests** (xUnit, unit + `WebApplicationFactory` integration tests against EF Core InMemory) covering auth, RBAC, verification rules, ownership enforcement, OTP lifecycle, media validation, scheme separation, the draft wizard, tracking, the clarification loop (including its background auto-close job), notification fan-out, the public-map privacy boundary, and every compliance flow (including both background sweeps); FluentValidation centralizes input validation with a consistent `422 validation_error` envelope |
| **Data integrity** | Every schema change is an additive EF Core migration (no destructive drops, across all 10 migrations); database-level unique constraints (case reference, admin email, reporter WhatsApp hash, reporter normalized email, storage path, device token); foreign keys with explicit delete behavior (Restrict/Cascade/SetNull per relationship) |
| **Maintainability** | Consistent per-feature folder structure (`Features/<Name>/{Dtos,Validators}`) mirrored on the mobile app (`src/lib/api/<feature>.ts`); a single documented error envelope (`{ error: { code, message, details } }`) shared byte-for-byte across the admin, WhatsApp, and mobile surfaces; structured logging via Serilog |

---

## 3.6 System Design

### 3.6.1 System Architecture

**Evidence:** actual deployed topology + backend project layering.

```
┌───────────────────────────┐      ┌───────────────────────────┐
│  Next.js 16 Admin Portal   │      │   Mobile App (Expo /       │
│  React 19 + TypeScript     │      │   React Native, Expo       │
│  shadcn/ui + Recharts      │      │   Router, NativeWind)      │
│  (frontend/, Vercel)       │      │   (mobile/)                │
└─────────────┬─────────────┘      └─────────────┬─────────────┘
              │ HTTPS, JWT — admin scheme         │ HTTPS, JWT — reporter scheme
              ▼                                   ▼
┌──────────────┐    ┌────────────────────────────────────────┐
│ Meta WhatsApp │───▶│        ASP.NET Core Web API             │
│ Cloud API     │    │        .NET 9  (backend/, Render)       │
│ (webhook)     │    │  Domain / Application / Infrastructure  │
└──────────────┘    │  / Api                                   │
                    │                                           │
                    │  ┌────────────────────────────────────┐  │
                    │  │ Two JWT schemes: Admin / Reporter    │  │
                    │  │ 3 periodic BackgroundServices:       │  │
                    │  │  clarification auto-close, data      │  │
                    │  │  export, account deletion +          │  │
                    │  │  retention purge                     │  │
                    │  └────────────────────────────────────┘  │
                    └───────┬──────────┬──────────┬────────────┘
                            │          │          │
              EF Core/Npgsql│          │ HTTP     │ HTTP
                            ▼          │ (service-│ (API key)
                ┌───────────────┐      │  role key)▼
                │  Supabase     │      ▼   ┌─────────────────────┐
                │  PostgreSQL   │  ┌────────────────┐  │  Resend (transactional │
                │  (Supabase)   │  │ Supabase Storage│  │  email — OTP, reset,   │
                └───────────────┘  │ (private bucket,│  │  welcome)               │
                                    │  signed URLs    │  └───────────────────────┘
                                    │  only — media    │
                                    │  + data exports)  │
                                    └────────────────┘
```

**Backend internal layering** (Clean/Onion architecture, 4 .NET projects):

| Layer | Project | Depends on | Contains |
| --- | --- | --- | --- |
| Domain | `CommunityIncidentReporting.Domain` | nothing | Entities, enums — zero framework dependency |
| Application | `CommunityIncidentReporting.Application` | Domain | DTOs, service interfaces, FluentValidation validators, feature-organized (`Features/<Name>/`) |
| Infrastructure | `CommunityIncidentReporting.Infrastructure` | Application, Domain | EF Core `AppDbContext` + migrations, service implementations, JWT/BCrypt security, external integrations (WhatsApp, Resend, Supabase Storage), 3 `BackgroundService` jobs |
| Api | `CommunityIncidentReporting.Api` | Application, Infrastructure, Domain | Controllers, middleware, DI wiring (`Program.cs`), Swagger |

**Technology stack** (from actual package/project files):

| Concern | Technology | Version |
| --- | --- | --- |
| Admin frontend framework | Next.js (App Router) | 16.3.0 |
| Admin frontend UI | React | 19.2.8 |
| Admin frontend language | TypeScript | ^5 |
| Admin frontend UI kit | shadcn/ui (Base UI primitives), Tailwind CSS | — / ^4 |
| Admin frontend charts | Recharts | 3.10.1 |
| Admin frontend data/forms | TanStack Query, React Hook Form, Zod | 5.101.4 / 7.85.0 / 4.4.3 |
| Admin frontend testing | Vitest, Playwright | 4.1.10 / 1.62.1 |
| Mobile app framework | Expo (React Native) | SDK ^54 |
| Mobile app runtime | React Native | 0.86.2 |
| Mobile app UI library | React | 19.2.3 |
| Mobile app language | TypeScript | ~6.0.3 |
| Mobile app routing | Expo Router (file-based, typed routes) | ~57.0.13 |
| Mobile app styling | NativeWind (Tailwind CSS for React Native) | ^4.2.6 |
| Mobile app device APIs | `expo-image-picker`, `expo-audio`, `expo-location`, `expo-secure-store`, `expo-device`, `@react-native-community/datetimepicker` | — |
| Mobile app state | React Context (`ReporterAuthProvider`, `ReportDraftProvider`) + a hand-rolled `fetch`-based API client — no external state-management library | — |
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
| Admin frontend hosting | Vercel | — |
| Mobile app distribution | Expo (development builds / EAS — not yet published to an app store) | — |

### 3.6.2 Use Case Diagram

**Evidence:** actors and use cases derived directly from controllers/policies (see 3.5.1
for the full verb list). Actor summary for the diagram:

**Actors:**
- **SuperAdmin** — every admin use case, plus administrator management, audit logs, settings
- **IncidentManager** — reports, assignment, verification decisions, categories, reporter restriction; no administrator/settings/audit-log access
- **Reviewer** — reports (limited update), verification decisions, notes; no assignment, categories, or reporter restriction
- **ReadOnlyAnalyst** — view-only: dashboard, reports, analytics
- **WhatsApp Reporter** *(anonymous/unauthenticated actor, external to the trust boundary)* — submit report via message; receive acknowledgment
- **Mobile Reporter** *(authenticated actor)* — register, verify email, log in, build and submit a draft report, track own reports, reply to clarification requests, view the public map, manage notifications/devices, manage privacy/profile/data-export/account-deletion
- **Public / anonymous map viewer** *(unauthenticated actor)* — view nearby verified incidents only, no other access
- **Meta WhatsApp Cloud API** *(external system actor)* — webhook subscription handshake, inbound message delivery
- **Resend** *(external system actor)* — outbound transactional email delivery
- **Supabase Storage** *(external system actor)* — media object and data-export storage

Use-case relationships worth showing explicitly: `«include»` "Verify Report" includes
"Record Verification Event" and conditionally "Update Case Status"; a Request
Clarification decision `«include»`s "Open Clarification Thread"; "Assign Report"
includes "Record Status History" when the prior status was `UnderReview`; "Submit Mobile
Report" and "Submit WhatsApp Report" both `«extend»` a common "Create Incident Report"
use case that produces the same downstream verification/case-management flow regardless
of channel; "Build Report Draft" `«include»`s "Upload Draft Evidence" and culminates in
"Submit Draft" which itself `«extend»`s "Create Incident Report"; "Request Account
Deletion" and the system-driven "Purge Inactive Reporter Data" both `«include»` a shared
"Anonymize Reporter" use case.

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

**Flow 3 — Mobile draft report + evidence, then submit**
1. `POST /api/mobile/reports/drafts` creates an empty draft owned by the caller.
2. `PATCH /api/mobile/reports/drafts/{id}` is called once per wizard step (category, then description/date/location, etc.) — full-replace semantics, the client resends the whole current draft state each time.
3. `POST /api/mobile/reports/drafts/{id}/attachments` (multipart) — validates MIME type against magic bytes (not just declared `Content-Type`), enforces per-type count/size limits, uploads to Supabase Storage under a draft-scoped path.
4. `POST /api/mobile/reports/drafts/{id}/submit` — validates every required field is present and the truth declaration was accepted; creates a real `IncidentReport` (`SourceChannel = MobileApp`, `Priority` from the category's default, never the client's suggestion), re-parents the draft's attachments onto it without re-uploading, and marks the draft submitted. Calling submit again on an already-submitted draft returns the same report rather than erroring or duplicating it.

**Flow 4 — Admin verification decision, with a clarification round-trip**
1. Admin authenticates (`POST /api/admin/auth/login`) and calls `GET /api/admin/verification-queue`.
2. `POST /api/admin/reports/{id}/verification-decision` with `{ action, reason }` — action ∈ Approve/Reject/RequestClarification/MarkDuplicate/Escalate.
3. Writes a `VerificationEvent` (attempt-numbered); on Approve, transitions `VerificationStatus → Verified` and `CaseStatus → UnderReview`; on RequestClarification, additionally opens a `ClarificationRequest` (with a reply deadline) and notifies the reporter; writes an `AuditLog` row either way.
4. If the reporter replies (`POST /api/mobile/clarifications/{id}/reply`) before the deadline, the request is marked resolved and the report stays visible in the admin's Needs-Clarification queue tab for a fresh decision. If nobody replies in time, a background job closes the report automatically (`CaseStatus → Closed`, `VerificationStatus → Rejected`) and notifies the reporter why.
5. Approved reports become visible in the operational `GET /api/admin/reports` queue for the first time (it is hard-scoped to `VerificationStatus == Verified`).

**Flow 5 — Anonymous public map query**
1. `GET /api/mobile/public/incidents?lat=&lng=&radiusM=&categories=` — no auth header at all.
2. A cheap bounding-box pre-filter (plain latitude/longitude arithmetic, SQL-translatable) narrows candidate reports server-side.
3. For each candidate, the query re-checks — live, not from a cached column alone — that the report is `Verified`, not `Withdrawn`, and that its reporter's `ShowOnPublicMap` privacy setting is on; an exact Haversine distance is then computed in memory over that already-small set and anything outside the requested radius is dropped.
4. Each remaining point is coarsened to ~1.1km precision unless that reporter opted into precise sharing, then returned with category, distance, and a relative-age bucket only — never reporter identity or free-text fields.

**Flow 6 — Account deletion and retention purge**
1. `DELETE /api/mobile/me` records an `AccountDeletionRequest` (`ScheduledForAt` = now + a configurable grace period) — nothing else changes yet; the reporter's session keeps working and `POST /api/mobile/me/deletion/cancel` cancels it any time before then.
2. A periodic background job finds requests whose grace period has passed and anonymizes the account: name/email/phone/password/WhatsApp identity scrubbed, every refresh token and device token revoked — but the reporter's own `IncidentReport` rows are left untouched, for audit/legal continuity.
3. A separate periodic job runs the same anonymization routine against any reporter whose last activity is older than `SystemSettings.ReporterDataRetentionMonths`, independent of whether they ever requested deletion themselves.

### 3.6.4 Database Design

**Evidence:** exact entity/field/relationship list from the EF Core model (25 tables,
`snake_case` table names, enums stored as `text` for tooling compatibility). All
tables/columns since the initial schema were added via additive migrations only — 10
migrations total, none of them destructive.

| Table | Key fields | Relationships |
| --- | --- | --- |
| `admin_users` | Id (PK), FullName, Email (unique), PasswordHash, Role (enum), IsActive, LastLoginAt, CreatedAt, UpdatedAt | 1—* `refresh_tokens`; 1—* `incident_reports` (AssignedAdmin); 1—* `report_assignments`, `status_histories`, `internal_notes`, `verification_events`, `audit_logs` (as actor), `clarification_requests` |
| `reporters` | Id (PK), WhatsAppNumberHash (unique, filtered), MaskedContactReference, FullName, Email (unique, filtered), NormalizedEmail, PhoneNumber, PasswordHash, EmailVerifiedAt, IsActive, RestrictionReason, LastLoginAt, LanguagePreference, TermsAcceptedAt/Version, **AnonymizedAt**, VerificationStatus, ConsentAt, IsRestricted, CreatedAt, UpdatedAt | 1—* `incident_reports`, `verification_events`, `reporter_refresh_tokens`, `reporter_consents`, `report_drafts`, `notifications`, `device_tokens`, `data_export_requests`, `account_deletion_requests`; 1—1 `reporter_privacy_settings` |
| `incident_categories` | Id (PK), Name (unique), Description, DefaultPriority (enum), SlaHours, IsActive, DisplayOrder, Slug (unique, filtered), IconKey, ColourToken, CreatedAt, UpdatedAt | 1—* `incident_reports`, `report_drafts` |
| `incident_reports` | Id (PK), CaseReference (unique, DB-sequence-generated e.g. `CIRS-2026-000001`), ReporterId (FK), CategoryId (FK), SourceChannel (enum), Description, IncidentOccurredAt, LocationDescription, Latitude, Longitude, Landmark, VerificationStatus (enum), CaseStatus (enum, incl. `Withdrawn`), Priority (enum), AssignedAdminId (FK, nullable), ResolutionSummary, WithdrawnAt/WithdrawalReason, DuplicateOfReportId (self-FK), **IsPubliclyVisible**, TruthDeclarationAcceptedAt, SubmittedAt, CreatedAt, UpdatedAt, ClosedAt | *—1 `reporters`, *—1 `incident_categories`, *—1 `admin_users` (assignee, SetNull); 1—* `verification_events`, `report_assignments`, `status_histories`, `internal_notes`, `incident_media_attachments`, `report_information_additions`, `clarification_requests`, `notifications` |
| `verification_events` | Id (PK), IncidentReportId (FK, cascade), ReporterId (FK), VerificationMethod (enum, incl. automated methods), Result (enum, incl. `AutoClosed`), AttemptNumber, Notes, PerformedByAdminId (FK, nullable = automated), CreatedAt | *—1 `incident_reports`, `reporters`, `admin_users` |
| `report_assignments` | Id (PK), IncidentReportId (FK, cascade), AdminUserId (FK), AssignedByAdminId (FK), AssignedAt, UnassignedAt | *—1 `incident_reports`, *—2 `admin_users` |
| `status_histories` | Id (PK), IncidentReportId (FK, cascade), PreviousStatus, NewStatus (enum), ChangedByAdminId (FK), Notes, CreatedAt | *—1 `incident_reports`, `admin_users` |
| `internal_notes` | Id (PK), IncidentReportId (FK, cascade), Content, CreatedByAdminId (FK), CreatedAt, UpdatedAt | *—1 `incident_reports`, `admin_users` |
| `audit_logs` | Id (PK), AdminUserId (FK, nullable = system action), Action, EntityType, EntityId, PreviousValueJson, NewValueJson, IpAddress, UserAgent, CreatedAt | *—1 `admin_users` (SetNull) |
| `refresh_tokens` | Id (PK), AdminUserId (FK, cascade), TokenHash (unique, SHA-256), ExpiresAt, RevokedAt, ReplacedByTokenHash, CreatedAt | *—1 `admin_users` — admin sessions only |
| `system_settings` | Id (PK, singleton), OrganizationName, OrganizationContactEmail, NotifyOnNewVerifiedReport, NotifyOnCriticalPriority, DefaultVerificationSlaHours, DuplicateDetectionWindowHours, ReporterDataRetentionMonths, AuditLogRetentionMonths, WhatsAppIntegrationEnabled, WhatsAppPlaceholderNote, UpdatedAt, UpdatedByAdminId | *—1 `admin_users` |
| `email_otp_verifications` | Id (PK), ReporterId (FK, nullable, SetNull), Email, Purpose (enum), CodeHash (HMAC), ExpiresAt, AttemptCount, MaxAttempts, IsUsed, UsedAt, CreatedAt, RequestIp, UserAgent | *—1 `reporters` (nullable) |
| `reporter_refresh_tokens` | Id (PK), ReporterId (FK, cascade), TokenHash (unique, SHA-256), ExpiresAt, IsRemembered, RevokedAt, ReplacedByTokenHash, CreatedAt | *—1 `reporters` — mobile reporter sessions only, separate from admin's `refresh_tokens` |
| `incident_media_attachments` | Id (PK), IncidentReportId (FK, cascade), FileName, StoragePath (unique), MediaType (enum), MimeType, FileSizeBytes, SortOrder, UploadedAt, UploadedByReporterId (nullable), IsDeleted, DeletedAt | *—1 `incident_reports` |
| `reporter_privacy_settings` *(Wave 2)* | Id (PK), ReporterId (FK, 1:1), UsePreciseLocation, ShowOnPublicMap, AllowResponderContact, UpdatedAt | 1—1 `reporters` |
| `reporter_consents` *(Wave 2)* | Id (PK), ReporterId (FK, cascade), ConsentType (enum), Granted, PolicyVersion, GrantedAt, RevokedAt | *—1 `reporters` — append-only history |
| `report_drafts` *(Wave 2)* | Id (PK), ReporterId (FK, cascade), CategoryId (FK, nullable), Description, IncidentOccurredAt, InitialPrioritySignal, LocationDescription, Latitude, Longitude, Landmark, SubmittedReportId (FK, nullable, unique), CreatedAt, UpdatedAt | *—1 `reporters`, `incident_categories`; 1—1 `incident_reports` once submitted; 1—* `report_draft_attachments` |
| `report_draft_attachments` *(Wave 2)* | Id (PK), ReportDraftId (FK, cascade), FileName, StoragePath (unique), MediaType, MimeType, FileSizeBytes, SortOrder, UploadedAt | *—1 `report_drafts` |
| `report_information_additions` *(Wave 2)* | Id (PK), IncidentReportId (FK, cascade), ReporterId (FK, cascade), Message, AttachmentId (FK, nullable, SetNull), CreatedAt | *—1 `incident_reports`, `reporters`, `incident_media_attachments` |
| `clarification_requests` *(Wave 2)* | Id (PK), IncidentReportId (FK, cascade), RequestedByAdminId (FK), Message, RequestedAt, DueAt, ResolvedAt, AutoClosedAt | *—1 `incident_reports`, `admin_users`; 1—* `clarification_responses` |
| `clarification_responses` *(Wave 2)* | Id (PK), ClarificationRequestId (FK, cascade), Message, AttachmentId (FK, nullable, SetNull), RespondedAt | *—1 `clarification_requests`, `incident_media_attachments` |
| `notifications` *(Wave 2)* | Id (PK), ReporterId (FK, cascade), Type (enum), Title, Body, ReportId (FK, nullable, cascade), ReadAt, CreatedAt | *—1 `reporters`, `incident_reports` |
| `device_tokens` *(Wave 2)* | Id (PK), ReporterId (FK, cascade), Platform (enum), Token (unique), LastSeenAt, RevokedAt, CreatedAt | *—1 `reporters` |
| `data_export_requests` *(Wave 2)* | Id (PK), ReporterId (FK, cascade), Status (enum), StoragePath, FailureReason, RequestedAt, CompletedAt | *—1 `reporters` |
| `account_deletion_requests` *(Wave 2)* | Id (PK), ReporterId (FK, cascade), Status (enum), RequestedAt, ScheduledForAt, CancelledAt, CompletedAt | *—1 `reporters` |

**Enumerations** (stored as `text`, not native Postgres enum types):

| Enum | Values |
| --- | --- |
| `AdminRole` | SuperAdmin, IncidentManager, Reviewer, ReadOnlyAnalyst |
| `VerificationStatus` | Pending, Verified, NeedsClarification, SuspectedDuplicate, FlaggedAbuse, Rejected |
| `CaseStatus` | VerificationPending, UnderReview, Assigned, InProgress, Resolved, Closed, Rejected, Duplicate, **Withdrawn** |
| `IncidentPriority` | Low, Medium, High, Critical |
| `SourceChannel` | WhatsApp, MobileApp |
| `VerificationMethod` | AdminReview, AutomatedDuplicateCheck, ReporterClarification, **AutomatedClarificationTimeout** |
| `VerificationDecisionResult` | Approved, Rejected, ClarificationRequested, MarkedDuplicate, Escalated, **AutoClosed** |
| `EmailOtpPurpose` | SignUpVerification, PasswordReset, EmailChange |
| `MediaType` | Image, Video, Audio, Document |
| `ConsentType` *(Wave 2)* | Location, Camera, Notifications, DataProcessing |
| `NotificationType` *(Wave 2)* | ClarificationRequested, ReportVerified, AssignmentMade, WorkStarted, ReportResolved, ReportRejected, ReportClosedDuplicate, ReportAutoClosed |
| `DevicePlatform` *(Wave 2)* | Ios, Android, Web |
| `DataExportStatus` *(Wave 2)* | Pending, Processing, Completed, Failed |
| `AccountDeletionStatus` *(Wave 2)* | Pending, Cancelled, Completed |

**Migration history** (all additive, chronological): `InitialCreate` →
`AddSystemSettings` → `AddReporterMobileAuthAndMedia` → `AddMobileWave2Reconciliation` →
`AddReporterConsentAndSessionRemember` → `AddReportDraftsAndCategoryCatalogue` →
`AddMobileReportsTrackingPhase4` → `AddClarificationLoopPhase5` →
`AddNotificationsPhase6` → `AddCompliancePhase8`.

For an ERD, draw the tables above as boxes with the listed fields, connecting FKs as
labeled relationship lines (1-to-many everywhere except the two explicit 1-to-1s noted
above; no many-to-many tables exist).

### 3.6.5 Interface (UI/UX) Design

**Evidence:** actual routed pages in the Next.js App Router (`frontend/src/app/`) and
the Expo Router file-based routes in the mobile app (`mobile/src/app/`).

**Admin web portal:**

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

**Mobile app** (Expo Router route groups: `(auth)` = unauthenticated, `(app)` =
authenticated; grounded in the actual `mobile/src/app/` file tree):

| Route | Screen |
| --- | --- |
| `(auth)/splash` | Launch screen |
| `(auth)/onboarding/privacy`, `/report`, `/track` | First-run explainer carousel (privacy stance, how to report, how to track) |
| `(auth)/sign-in` | Login |
| `(auth)/create-account` | Registration |
| `(auth)/verify-email` | 6-digit OTP entry |
| `(auth)/consent` | Consent capture (location/camera/notifications/data-processing grants) |
| `(app)/(tabs)/home` | Home tab |
| `(app)/report/category` → `/details` → `/evidence` → `/location` → `/review` → `/submitted` | The 5-step report wizard (draft-backed, resumable) ending in a submitted confirmation screen |
| `(app)/(tabs)/my-reports` | Own report list, filterable by status bucket |
| `(app)/(tabs)/my-reports/[id]` | Report detail + timeline |
| `(app)/(tabs)/my-reports/[id]/clarification` | Clarification thread view/reply |
| `(app)/(tabs)/nearby-incidents` | The public map |
| `(app)/(tabs)/notifications` | Notification inbox |
| `(app)/(tabs)/profile` | Account/profile home |
| `(app)/(tabs)/profile/privacy` | Privacy settings (public-map visibility, location precision, contact-ability) |

**Design system evidence — admin portal:** `shadcn/ui` (Base UI primitives) on a custom
"public-safety" palette (slate/off-white content area, deep navy sidebar, one muted-blue
primary, reserved green/amber/red semantic colors) applied via CSS variables; a validated
colorblind-safe categorical chart palette (a hand-picked one was replaced after failing
CVD/normal-vision separation checks); role-aware navigation (SuperAdmin-only links
hidden, and an explicit "access restricted" state shown on direct navigation as
defense-in-depth); confirmation dialogs requiring a reason for consequential actions
(status changes, verification decisions); keyboard accessibility fixes documented in
Phase 8 (audit-log rows made keyboard-operable).

**Design system evidence — mobile app:** NativeWind (Tailwind CSS utility classes
applied to React Native primitives) for styling; a shared component library
(`src/components/ui/`: buttons, text fields, OTP input, password-strength meter,
segmented control, status badges, toggle rows, avatars) reused across every screen
rather than each screen styling its own controls; `expo-secure-store` for on-device
token storage; a `ReportDraftProvider` React Context that persists wizard progress
across the 5 report-submission steps so navigating back and forth (or backgrounding the
app) never loses in-progress input.

---

## Quick reference: source files if you need to go deeper

- `docs/architecture.md` — original architecture write-up
- `docs/api-contract.md` — full admin API endpoint table
- `docs/mobile-api-contract.md` — full mobile API endpoint table, including every Wave 2 addition
- `docs/mobile-client-backend-extension.md` — mobile extension design record, phase-by-phase (Wave 1 and Wave 2)
- `docs/whatsapp-integration-plan.md` — WhatsApp webhook contract
- `README.md` — Wave 1 phase-by-phase build history, tech stack, deployment
- `backend/src/CommunityIncidentReporting.Domain/Entities/` — every entity definition
- `backend/src/CommunityIncidentReporting.Infrastructure/Persistence/Migrations/` — full migration history
- `mobile/src/app/` — every mobile screen (file-based routing — the file tree above is exhaustive)
- `mobile/src/lib/api/` — the mobile app's typed API client, one file per backend feature area
