# Mobile API Contract

Base URL (local): `http://localhost:5058`. All routes below are prefixed `/api/mobile`.
This is a separate surface from `/api/admin/...` (see [`docs/api-contract.md`](api-contract.md))
with its own JWT scheme — a reporter access token issued by `/mobile/auth/*` will never
authorize an `/api/admin/...` request, and an admin access token will never authorize an
`/api/mobile/...` request, even though both are ordinary `Authorization: Bearer <token>`
headers. See [`mobile-client-backend-extension.md`](mobile-client-backend-extension.md)
for why (two independently-keyed ASP.NET Core JWT Bearer schemes).

Responses use the same envelope as the admin API:

- Success: the resource, or `{ items, total, page, pageSize }` for paginated lists.
- Error: `{ "error": { "code": string, "message": string, "details"?: object } }`.
  Validation errors use `code: "validation_error"` with per-field `details` (HTTP 422).

| HTTP status | `error.code` | Meaning |
| --- | --- | --- |
| 400 | `invalid_otp` | OTP missing/wrong/expired/already-used/attempt-limited — always this one generic code and message, deliberately never more specific |
| 401 | `invalid_credentials` | Login failed, or a refresh/access token is invalid, expired, revoked, or wrong scheme |
| 404 | `not_found` | Resource doesn't exist, or exists but isn't owned by the caller — the two are indistinguishable on purpose |
| 409 | `business_rule_violation` | e.g. email already registered and verified, attachment limits exceeded, report no longer mutable |
| 422 | `validation_error` | Request body failed FluentValidation |
| 429 | (rate limiter, no body) | Too many requests to an `auth`/`otp`-limited endpoint within the window |
| 502 | `email_delivery_failed` / `storage_error` | Resend or Supabase Storage rejected/couldn't be reached |

## Auth (`/mobile/auth`)

All endpoints here are anonymous unless marked "reporter token required". Register,
login, OTP verify/resend, and password-reset endpoints are rate-limited by client IP
(named policies `auth`/`otp` in `Program.cs`).

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| POST | `/register` | `{ fullName, email, phoneNumber, password, confirmPassword, consentAccepted }` | `{ reporterId, email, verificationRequired: true, message }` — no session issued |
| POST | `/verify-email-otp` | `{ email, otpCode }` | `ReporterAuthTokenResponse` (activates the account, sets `EmailVerifiedAt`, issues a session) |
| POST | `/resend-email-otp` | `{ email }` | `{ message }` — always the same generic message, whether or not the email exists or is already verified |
| POST | `/login` | `{ email, password }` | `ReporterAuthTokenResponse` — only for verified, active, non-restricted accounts; wrong password and "no such account" return the identical `invalid_credentials` error |
| POST | `/refresh` | `{ refreshToken }` | `ReporterAuthTokenResponse` — rotates the refresh token (old one is revoked) |
| POST | `/logout` | `{ refreshToken }` *(reporter token required)* | 204 No Content |
| GET | `/me` | — *(reporter token required)* | `ReporterProfileDto` |
| POST | `/forgot-password` | `{ email }` | `{ message }` — always generic, never reveals whether the email is registered |
| POST | `/verify-password-reset-otp` | `{ email, otpCode }` | `{ message }` — validates the code (attempt-counted) without consuming it |
| POST | `/reset-password` | `{ email, otpCode, newPassword, confirmNewPassword }` | `{ message }` — re-validates and *consumes* the code, sets the new password, revokes every existing reporter session for that account |

`RegisterReporterRequest` validation: `fullName`/`email` required; `phoneNumber` must
match `^\+?[1-9]\d{6,14}$`; `password` ≥ 8 chars with at least one letter and one digit;
`confirmPassword` must equal `password`; `consentAccepted` must be `true`.
`otpCode` is always exactly 6 digits.

```
ReporterProfileDto { id, fullName, email, phoneNumber, emailVerified, isActive, isRestricted, lastLoginAt, createdAt }
ReporterAuthTokenResponse { accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, reporter: ReporterProfileDto }
```

Never in any response: `PasswordHash`, OTP codes (raw or hashed), refresh token hashes,
or another reporter's data.

## Reports (`/mobile/reports`)

Every endpoint requires a reporter access token. A report or attachment not owned by the
caller returns `404 not_found` — identical to it not existing.

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| POST | `/` | `CreateMobileReportRequest` | `MobileReportDetailDto` |
| GET | `/?page=&pageSize=` | — | `{ items: MobileReportListItemDto[], total, page, pageSize }` — only the caller's own reports |
| GET | `/{id}` | — | `MobileReportDetailDto` |
| POST | `/{id}/attachments` | `multipart/form-data`, one or more `files` parts | `MediaAttachmentDto[]` for the files just uploaded |
| DELETE | `/{reportId}/attachments/{attachmentId}` | — | 204 No Content (soft-delete) |
| GET | `/{reportId}/attachments/{attachmentId}/access-url` | — | `SignedUrlResponse { url, expiresAt }` |

```
CreateMobileReportRequest { categoryId, description, incidentOccurredAt, locationDescription, latitude?, longitude?, initialPrioritySignal? }
MobileReportListItemDto { id, caseReference, categoryName, createdAt, priority, verificationStatus, caseStatus, attachmentCount }
MobileReportDetailDto {
  id, caseReference, categoryId, categoryName, sourceChannel, description, incidentOccurredAt,
  locationDescription, latitude, longitude, verificationStatus, caseStatus, priority,
  resolutionSummary, createdAt, updatedAt, closedAt,
  statusHistory: [{ previousStatus, newStatus, createdAt }],   // no notes, no actor identity
  attachments: MediaAttachmentDto[]
}
MediaAttachmentDto { id, fileName, mediaType, mimeType, fileSizeBytes, sortOrder, uploadedAt }   // never storagePath or a URL
```

Server-controlled fields a client can never set: `reporterId` (from the JWT `sub`
claim), `sourceChannel` (always `MobileApp`), `caseReference` (DB sequence), `priority`
(always the category's `DefaultPriority` — `initialPrioritySignal` is recorded in the
audit log only, never applied), `verificationStatus`/`caseStatus` (always start
`Pending`/`VerificationPending`).

`MobileReportDetailDto` deliberately omits `InternalNotes`, the `AuditLog` trail, and
`AssignedAdmin` identity — those are admin-only. Every mobile report flows through the
same verification queue, case-status transitions, assignment, and audit logging as a
WhatsApp report (see `IVerificationService`/`IncidentReportService`); nothing about that
pipeline is mobile-specific.

### Attachment upload rules

- Multipart form field name: `files` (repeatable for multiple files in one request).
- Allowed MIME types (checked against actual file content via magic bytes, not just the
  declared `Content-Type` or filename extension): `image/jpeg`, `image/png`,
  `image/webp`, `video/mp4`, `video/quicktime`, `audio/mpeg`, `audio/mp4`, `audio/wav`,
  `application/pdf`. Anything else, or content that doesn't match its declared type,
  returns `409 business_rule_violation`.
- Per-report limits (configurable, see `backend/.env.example`'s `MediaUpload__*`,
  defaults shown): max 5 images, 2 videos, 1 audio file, 3 documents; max size 10MB
  (image/document), 100MB (video), 20MB (audio). Limits apply cumulatively — an upload
  that would push a report over any per-type count returns `409` and stores nothing from
  that request.
- A report only accepts attachment changes (upload or delete) while its `caseStatus` is
  `VerificationPending` or `UnderReview` — once an admin has assigned or progressed it,
  attachments are locked (`409 business_rule_violation`). This mirrors "before the
  report enters a restricted review state."
- Storage write ordering: the file is uploaded to Supabase Storage first; the database
  row is only written after that succeeds. If one file in a multi-file request fails
  (validation or storage), any files already stored earlier in the same request are
  deleted and nothing from that request is persisted.
- `GET .../access-url` issues a fresh short-lived signed URL (`MediaUpload__SignedUrlExpirySeconds`,
  default 300s) on every call — there is no permanent public URL for any attachment.

## Admin-side additions

These extend the existing `/api/admin/...` surface (unchanged auth/contract otherwise —
see [`docs/api-contract.md`](api-contract.md)):

- `GET /api/admin/reports` accepts a new `sourceChannel` query parameter
  (`WhatsApp` | `MobileApp`).
- `IncidentReportListItemDto` and `IncidentReportDetailDto` both gained a `sourceChannel`
  field; `IncidentReportDetailDto` also gained `mediaAttachments: MediaAttachmentDto[]`
  (metadata only).
- `VerificationQueueItemDto` gained `sourceChannel` — the verification queue itself
  already covers both channels with no other change (it was never scoped to a channel).
- New endpoint: `GET /api/admin/reports/{id}/attachments/{attachmentId}/access-url` →
  `SignedUrlResponse`, `ReviewerOrAbove` policy, no reporter-ownership check (any
  authorized admin may view any report's attachments).
- `DashboardMetricsDto` gained `reportsBySourceChannel: NamedCountDto[]`.
- `AnalyticsResponse` gained `resolvedBySourceChannel: NamedCountDto[]` and
  `verificationOutcomesBySourceChannel: { sourceChannel, result, count }[]`.

All of the above are additive fields/endpoints — no existing admin response shape or
route changed, so the deployed Vercel frontend's existing calls are unaffected.
