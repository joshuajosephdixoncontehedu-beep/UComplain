# API Contract

Base URL (local): `http://localhost:5058`. All routes below are prefixed `/api/admin`.
All endpoints except `POST /auth/login` and `POST /auth/refresh` require a valid JWT
bearer token (`Authorization: Bearer <token>`). Role columns show which `AdminRole`
values are authorized; "any" means any authenticated administrator.

Responses use a consistent envelope:

- Success: the resource (or `{ items, total, page, pageSize }` for paginated lists).
- Error: `{ "error": { "code": string, "message": string, "details"?: object } }` — see
  the global error-handling middleware added in Phase 1. Validation errors use
  `code: "validation_error"` with per-field `details`.

This document is written before the endpoints are implemented (Phase 0) and is kept in
sync as Phases 2–3 land.

## Auth

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| POST | `/auth/login` | public | `{ email, password }` → `{ accessToken, refreshToken, expiresAt, admin }` |
| POST | `/auth/refresh` | public (valid refresh token) | `{ refreshToken }` → new token pair |
| POST | `/auth/logout` | any | revokes the current refresh token |
| GET | `/auth/me` | any | returns the current admin's profile + role |

No public registration endpoint exists. Administrator accounts are created only by a
`SuperAdmin` via `POST /administrators`.

## Dashboard

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| GET | `/dashboard?from=&to=` | any | metric cards, trend deltas, chart series for the date range |

## Reports

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| GET | `/reports` | any | paginated, filterable, sortable; defaults to `VerificationStatus = Verified` only |
| GET | `/reports/{id}` | any | full detail incl. masked reporter contact, decision history, notes, audit timeline |
| PATCH | `/reports/{id}` | IncidentManager, Reviewer (limited), SuperAdmin | updates mutable fields (priority, category, location, description); writes `StatusHistory`/`AuditLog` as applicable |
| POST | `/reports/{id}/assign` | IncidentManager, SuperAdmin | assigns/reassigns an admin; writes `ReportAssignment` + `AuditLog` |
| POST | `/reports/{id}/notes` | IncidentManager, Reviewer, SuperAdmin | adds an `InternalNote` |
| POST | `/reports/{id}/status` | IncidentManager, SuperAdmin (Reviewer limited) | transitions `CaseStatus`; validates legal transitions; writes `StatusHistory` + `AuditLog` |

Query parameters for `GET /reports`: `page`, `pageSize`, `search`, `categoryId`,
`priority`, `caseStatus`, `verificationStatus`, `assignedAdminId`, `location`, `from`,
`to`, `sortBy`, `sortDir`.

## Verification

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| GET | `/verification-queue` | IncidentManager, Reviewer, SuperAdmin | reports with `VerificationStatus != Verified`, grouped by status tab |
| POST | `/reports/{id}/verification-decision` | IncidentManager, Reviewer, SuperAdmin | `{ action, reason? }`; writes `VerificationEvent` + `AuditLog` |

`action` ∈ `approve | reject | request_clarification | mark_duplicate | escalate`.
Approving sets `VerificationStatus = Verified` (report becomes eligible for the
operational queue); the other actions keep it out of the operational queue. No action
produces an automated "confirmed false" verdict — that judgment is always the
authorized human's `reason` text, recorded on the `VerificationEvent`.

## Users (reporters)

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| GET | `/users` | any | paginated reporter list (masked contact reference only) |
| GET | `/users/{id}` | any | reporter detail: report history, verification history, consent, restriction status |
| POST | `/users/{id}/restrict` | IncidentManager, SuperAdmin | sets `IsRestricted = true`; writes `AuditLog` |
| POST | `/users/{id}/unrestrict` | IncidentManager, SuperAdmin | sets `IsRestricted = false`; writes `AuditLog` |

## Administrators

SuperAdmin-only.

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/administrators` | list with role/status |
| POST | `/administrators` | creates an admin account (`FullName`, `Email`, `Role`, temp password); writes `AuditLog` |
| PATCH | `/administrators/{id}` | updates name/role; writes `AuditLog` |
| POST | `/administrators/{id}/deactivate` | sets `IsActive = false`; blocked if it's the last active `SuperAdmin` |
| POST | `/administrators/{id}/reactivate` | sets `IsActive = true`; writes `AuditLog` |

## Categories

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| GET | `/categories` | any | ordered by `DisplayOrder` |
| POST | `/categories` | IncidentManager, SuperAdmin | creates a category with default priority + SLA hours |
| PATCH | `/categories/{id}` | IncidentManager, SuperAdmin | updates fields incl. `DisplayOrder` |
| POST | `/categories/{id}/disable` | IncidentManager, SuperAdmin | sets `IsActive = false` (soft disable, not delete) |

## Analytics and audit

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| GET | `/analytics?from=&to=` | any | chart series, assignment workload, response-time stats; CSV export via `?format=csv` |
| GET | `/audit-logs` | SuperAdmin | filterable by actor, action, entity type, date range |

## Settings

| Method | Route | Role | Notes |
| --- | --- | --- | --- |
| GET | `/settings` | SuperAdmin | organisation, notification, verification-rule, privacy/retention, WhatsApp-placeholder settings |
| PATCH | `/settings` | SuperAdmin | updates settings; writes `AuditLog` |

## Every state-changing endpoint

Every `POST`/`PATCH` endpoint above:

1. Validates the caller's role against the route's policy (403 if not authorized).
2. Validates the request body (422 `validation_error` on failure).
3. Applies the change transactionally.
4. Writes an `AuditLog` row (`AdminUserId`, `Action`, `EntityType`, `EntityId`,
   `PreviousValueJson`, `NewValueJson`, `IpAddress`, `UserAgent`).
5. Returns the updated resource.
