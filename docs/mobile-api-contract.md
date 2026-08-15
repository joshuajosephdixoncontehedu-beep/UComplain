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
| POST | `/login` | `{ email, password, rememberMe? }` | `ReporterAuthTokenResponse` — only for verified, active, non-restricted accounts; wrong password and "no such account" return the identical `invalid_credentials` error |
| POST | `/refresh` | `{ refreshToken }` | `ReporterAuthTokenResponse` — rotates the refresh token (old one is revoked) |
| POST | `/logout` | `{ refreshToken }` *(reporter token required)* | 204 No Content |
| GET | `/me` | — *(reporter token required)* | `ReporterProfileDto` |
| POST | `/forgot-password` | `{ email }` | `{ message }` — always generic, never reveals whether the email is registered |
| POST | `/verify-password-reset-otp` | `{ email, otpCode }` | `{ message }` — validates the code (attempt-counted) without consuming it |
| POST | `/reset-password` | `{ email, otpCode, newPassword, confirmNewPassword }` | `{ message }` — re-validates and *consumes* the code, sets the new password, revokes every existing reporter session for that account |
| POST | `/consent` | `{ consents: [{ consentType, granted, policyVersion }, ...] }` *(reporter token required)* | `ConsentDto[]` — records one row per grant in the batch |

`rememberMe` defaults `true` (a `Jwt__ReporterRefreshTokenDays`-lived refresh token, 30
days by default); `false` issues a short one instead (`Jwt__ReporterShortSessionHours`,
12 hours by default). The category is fixed at issuance and carried through every
subsequent `/refresh` rotation — a short session can never silently become long-lived.

`consentType` ∈ `Location | Camera | Notifications | DataProcessing`. Consent is
append-only: submitting the same `consentType` again always adds a new row rather than
overwriting the previous one, so a reporter's consent history stays fully auditable —
the current state for a type is whichever row has the latest `grantedAt`.

`RegisterReporterRequest` validation: `fullName`/`email` required; `phoneNumber` must
match `^\+?[1-9]\d{6,14}$`; `password` ≥ 8 chars with at least one letter and one digit;
`confirmPassword` must equal `password`; `consentAccepted` must be `true`.
`otpCode` is always exactly 6 digits.

```
ReporterProfileDto { id, fullName, email, phoneNumber, emailVerified, isActive, isRestricted, lastLoginAt, createdAt, languagePreference }
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
| GET | `/?page=&pageSize=&status=` | — | `{ items: MobileReportListItemDto[], total, page, pageSize }` — only the caller's own reports |
| GET | `/counts` | — | `ReportCountsDto` — bucket counts for the caller's own reports |
| GET | `/{id}` | — | `MobileReportDetailDto` |
| GET | `/{id}/timeline` | — | `MobileReportStatusHistoryDto[]`, chronological (oldest first) |
| GET | `/{id}/information` | — | `ReportInformationDto[]`, chronological (oldest first) |
| POST | `/{id}/information` | `AddReportInformationRequest` | `ReportInformationDto` |
| GET | `/{id}/clarifications` | — | `ClarificationRequestDto[]`, chronological (oldest first) — see "Clarifications" below |
| POST | `/{id}/withdraw` | `WithdrawReportRequest` | `MobileReportDetailDto` |
| POST | `/{id}/attachments` | `multipart/form-data`, one or more `files` parts | `MediaAttachmentDto[]` for the files just uploaded |
| DELETE | `/{reportId}/attachments/{attachmentId}` | — | 204 No Content (soft-delete) |
| GET | `/{reportId}/attachments/{attachmentId}/access-url` | — | `SignedUrlResponse { url, expiresAt }` |

```
CreateMobileReportRequest { categoryId, description, incidentOccurredAt, locationDescription, latitude?, longitude?, initialPrioritySignal?, landmark? }
MobileReportListItemDto {
  id, caseReference, categoryName, createdAt, priority, verificationStatus, caseStatus, attachmentCount,
  statusBadge, trackerStage, progressPercent, bucket   // ReportStatusProjection fields — see below
}
MobileReportDetailDto {
  id, caseReference, categoryId, categoryName, sourceChannel, description, incidentOccurredAt,
  locationDescription, latitude, longitude, landmark, verificationStatus, caseStatus, priority,
  resolutionSummary, createdAt, updatedAt, closedAt, withdrawnAt, withdrawalReason,
  statusHistory: [{ previousStatus, newStatus, createdAt }],   // no notes, no actor identity
  attachments: MediaAttachmentDto[]
}
MediaAttachmentDto { id, fileName, mediaType, mimeType, fileSizeBytes, sortOrder, uploadedAt }   // never storagePath or a URL
ReportCountsDto { active, resolved, rejected, total }   // total counts every report, including any Withdrawn ones — the other three don't always sum to it
ReportInformationDto { id, message, attachmentId, createdAt }
AddReportInformationRequest { message, attachmentId? }   // attachmentId, if set, must already exist on this same report
WithdrawReportRequest { reason }
```

`bucket` ∈ `Active | Resolved | Rejected | NotListed` (`NotListed` = a withdrawn report —
not shown in any of the app's default list tabs, but still reachable via `?status=` or
`GET /{id}` directly). `status` on `GET /reports` accepts `Active | Resolved | Rejected`;
omitting it returns every one of the caller's own reports regardless of bucket.

`POST /{id}/information` only succeeds while the report's `bucket` is `Active`
(`409 business_rule_violation` otherwise — a resolved, rejected, or withdrawn report is
done). It never accepts a new file upload — `attachmentId` can only reference something
already uploaded to this report while it was still mutable (see "Attachment upload
rules" below); a non-existent or foreign attachment id returns `404 not_found`.

`POST /{id}/withdraw` only succeeds while `caseStatus` is `VerificationPending`,
`UnderReview`, or `Assigned` (`409 business_rule_violation` otherwise — once an officer
has moved a report to `InProgress` or further, only they can change it from there).
`WithdrawReportRequest.reason` is required (max 500 chars) and is stored on the report as
`WithdrawalReason`, alongside a `WithdrawnAt` timestamp — both readable on
`MobileReportDetailDto`.

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

## Categories (`/mobile/categories`)

Requires a reporter access token (same as every other `/api/mobile/...` route — there is
no anonymous catalogue endpoint).

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| GET | `/` | — | `MobileCategoryDto[]` — only `IsActive` categories, ordered by `DisplayOrder` |

```
MobileCategoryDto { id, name, slug, iconKey, colourToken, defaultPriority, displayOrder }
```

`slug`/`iconKey`/`colourToken` are nullable — populated per-category via the existing
admin Categories page (see `docs/api-contract.md`); a category with none of them set
still appears, just without client-side icon/colour hints.

## Report drafts (`/mobile/reports/drafts`)

The step-by-step report wizard's server-side backing store. A draft holds partial state
across steps and its own attachments, entirely separate from `IncidentReport` until
`submit` succeeds. All routes below require a reporter access token and are scoped under
`/mobile/reports` (so full paths are `/api/mobile/reports/drafts...`); `"drafts"` never
collides with `GET /reports/{id}` because `"drafts"` isn't a valid GUID.

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| POST | `/drafts` | — | `DraftDto` — a new, empty draft owned by the caller |
| PATCH | `/drafts/{id}` | `UpdateDraftRequest` | `DraftDto` |
| POST | `/drafts/{id}/attachments` | `multipart/form-data`, one or more `files` parts | `MediaAttachmentDto[]` for the files just uploaded |
| DELETE | `/drafts/{draftId}/attachments/{attachmentId}` | — | 204 No Content |
| POST | `/drafts/{id}/submit` | `SubmitDraftRequest` | `MobileReportDetailDto` — the newly created (or, on retry, the already-created) report |

```
DraftDto {
  id, categoryId, categoryName, description, incidentOccurredAt, initialPrioritySignal,
  locationDescription, latitude, longitude, landmark, submittedReportId,
  createdAt, updatedAt, attachments: MediaAttachmentDto[]
}
UpdateDraftRequest { categoryId?, description?, incidentOccurredAt?, initialPrioritySignal?, locationDescription?, latitude?, longitude?, landmark? }
SubmitDraftRequest { truthDeclarationAccepted }
```

`PATCH .../drafts/{id}` is **full-replace, not a partial diff** — same convention as the
admin side's `UpdateReportRequest`. Send the wizard's complete current state on every
step, not just the field that just changed; omitted fields are cleared to `null`.

Draft attachment upload/delete reuses the exact same validation, magic-byte MIME
checking, and per-type/size limits as `/reports/{id}/attachments` (see "Attachment
upload rules" above) — storage path prefix differs
(`incident-report-drafts/{draftId}/{attachmentId}/{filename}`) but the rules are
identical.

`submit` requires `truthDeclarationAccepted: true` (else `422 validation_error`) and
every one of `categoryId`, `description`, `incidentOccurredAt`, `locationDescription`
already set on the draft (else `409 business_rule_violation` — the wizard isn't
complete). On success it creates a real `IncidentReport` (server-controlled fields exactly
as in `POST /reports`, including `priority` from the category's `DefaultPriority` — the
draft's `initialPrioritySignal` is audit-logged only, never applied), re-parents the
draft's attachments onto it (no re-upload — same `StoragePath`, new
`IncidentMediaAttachment` rows), and keeps the draft row (rather than deleting it),
recording `submittedReportId` on it. **`submit` is idempotent**: calling it again on an
already-submitted draft returns `200 OK` with the same report rather than erroring or
creating a duplicate — the draft's own `submittedReportId` is the idempotency boundary,
so no separate idempotency-key header or field is needed. Any further `PATCH` to an
already-submitted draft returns `409 business_rule_violation`.

## Clarifications

A `ClarificationRequest` is created automatically whenever an admin records a
`RequestClarification` verification decision on the admin side (see
[`docs/api-contract.md`](api-contract.md)) — there is no mobile endpoint that creates
one directly. A report can accumulate several over its lifetime (each new
`RequestClarification` decision adds another). Listing a thread is under `/reports/{id}`
(above); replying is its own top-level surface since a reply only needs the
`ClarificationRequestId`, not the report id.

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| POST | `/clarifications/{id}/reply` | `ReplyToClarificationRequest` | `ClarificationResponseDto` |

```
ClarificationRequestDto { id, message, requestedAt, dueAt, resolvedAt, autoClosedAt, responses: ClarificationResponseDto[] }
ClarificationResponseDto { id, message, attachmentId, respondedAt }
ReplyToClarificationRequest { message, attachmentId? }   // attachmentId, if set, must already exist on the underlying report
```

`resolvedAt` is set on the reporter's **first** reply to a request (later back-and-forth
on the same request doesn't re-resolve anything). `autoClosedAt` is set only if nobody
ever replied before `dueAt` — the two are mutually exclusive in practice. A reply only
succeeds while the underlying report's `verificationStatus` is still
`NeedsClarification` (`409 business_rule_violation` otherwise) — this single check
covers both "an admin already re-decided the report through another path" and "the
deadline already passed and it was auto-closed," since both move `verificationStatus`
away from `NeedsClarification`.

If nobody replies before `dueAt` (`Clarification:DeadlineHours`, default 48 hours after
the request was made), a background sweep closes the report automatically: `caseStatus`
becomes `Closed`, `verificationStatus` becomes `Rejected` (so it reads as "Rejected" on
the mobile status badge and drops out of the admin's NeedsClarification queue tab, into
the Rejected one), and `resolutionSummary` explains why.

## Notifications (`/mobile/notifications`) and devices (`/mobile/devices`)

Persistence and listing only — **there is no actual push send yet** (no APNs/FCM
delivery). A `Notification` row is created automatically at the relevant points on the
admin/system side (see `docs/mobile-client-backend-extension.md`'s Phase 6 section for
the exact fan-out list); there is no endpoint that creates one directly.

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| GET | `/notifications?page=&pageSize=` | — | `{ items: NotificationDto[], total, page, pageSize }`, newest first |
| POST | `/notifications/{id}/read` | — | `NotificationDto` |
| POST | `/notifications/read-all` | — | `MarkAllReadResponse` |
| POST | `/devices` | `RegisterDeviceRequest` | `DeviceTokenDto` |
| DELETE | `/devices/{id}` | — | 204 No Content |

```
NotificationDto { id, type, title, body, reportId, readAt, createdAt }
MarkAllReadResponse { updatedCount }
RegisterDeviceRequest { platform, token }   // platform ∈ Ios | Android | Web
DeviceTokenDto { id, platform, lastSeenAt }
```

`type` ∈ `ClarificationRequested | ReportVerified | AssignmentMade | WorkStarted |
ReportResolved | ReportRejected | ReportClosedDuplicate | ReportAutoClosed` — drives the
client's icon/grouping only, never anything server-side.

Marking read is idempotent — reading an already-read notification again returns the same
`readAt`, it doesn't move it forward. Revoking an already-revoked device is likewise a
no-op, not an error.

`POST /devices` is an **upsert by `token`**, not create-or-conflict: registering a token
already on file reassigns it to the calling reporter and refreshes
`lastSeenAt`/`platform` — the same push token naturally moves between accounts on a
shared device or after a reinstall/relogin, so the response's `id` may be an existing
row's, not a new one.

## Public map (`/mobile/public/incidents`)

**The one endpoint under `api/mobile/...` that takes no reporter token at all** — every
other route on this page requires one. Anonymous, unauthenticated, safe to call from an
unauthenticated map screen.

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| GET | `/?lat=&lng=&radiusM=&categories=` | — | `PublicIncidentDto[]`, nearest first |

`lat`/`lng` are required query params (plain numbers, degrees). `radiusM` is optional
(default 5000, clamped server-side to [100, 20000] regardless of what's requested).
`categories` is an optional comma-separated list of category ids — omit for every
category. Results are capped at 500.

```
PublicIncidentDto { id, categoryName, categoryIconKey, categoryColourToken, latitude, longitude, distanceMeters, ageBucket }
```

`ageBucket` ∈ `Today | ThisWeek | ThisMonth | Older` — a coarse relative age from the
report's `createdAt`, never the exact timestamp. `latitude`/`longitude` are rounded to 2
decimal places (~1.1km) instead of the true coordinates when the reporter's
`UsePreciseLocation` privacy setting is off.

**Deliberately never returned**: reporter identity (id, masked contact, anything),
`description`, `locationDescription`, or any other free-text field a reporter might have
put identifying details into. Only `id`, category, coarse/precise position, distance, and
age bucket.

A report only ever appears here when **all** of the following hold, checked directly by
this query (not solely inferred from the report's own cached `isPubliclyVisible` flag):
`verificationStatus == Verified`, `caseStatus != Withdrawn`, and the reporter's own
`ShowOnPublicMap` privacy setting is `true` (the default when a reporter has never set
one). A withdrawn report drops off the map immediately, even though withdrawing never
changes `verificationStatus` away from `Verified`.

## Me / compliance (`/mobile/me`)

Reporter self-service account management. Every route requires a reporter access token.

| Method | Route | Request | Response |
| --- | --- | --- | --- |
| GET | `/privacy` | — | `ReporterPrivacySettingDto` (get-or-create — defaults `true`/`true`/`true` on first call) |
| PUT | `/privacy` | `UpdateReporterPrivacySettingRequest` | `ReporterPrivacySettingDto` |
| GET | `/stats` | — | `ReporterStatsDto` |
| PATCH | `/` | `UpdateMyProfileRequest` | `ReporterProfileDto` |
| POST | `/data-export` | — | `DataExportRequestDto` |
| GET | `/data-export` | — | `DataExportRequestDto` |
| DELETE | `/` | — | `AccountDeletionRequestDto` |
| POST | `/deletion/cancel` | — | `AccountDeletionRequestDto` |

```
ReporterPrivacySettingDto { usePreciseLocation, showOnPublicMap, allowResponderContact, updatedAt }
UpdateReporterPrivacySettingRequest { usePreciseLocation, showOnPublicMap, allowResponderContact }   // full-replace, like every other PATCH/PUT here
ReporterStatsDto { activeReports, resolvedReports, rejectedReports, totalReports, memberSince }
UpdateMyProfileRequest { fullName, languagePreference? }   // full-replace; languagePreference: null clears it
DataExportRequestDto { id, status, requestedAt, completedAt, downloadUrl, downloadUrlExpiresAt, failureReason }
AccountDeletionRequestDto { id, status, requestedAt, scheduledForAt, cancelledAt }
```

**Privacy**: `PUT .../privacy` with `showOnPublicMap: false` removes every one of the
caller's own existing reports from `GET /api/mobile/public/incidents` immediately, not
just future submissions — see that endpoint's own visibility rules.

**Stats**: the exact same bucket counts as `GET /api/mobile/reports/counts`
(`ReportCountsDto`), plus `memberSince` (the account's `createdAt`).

**Data export**: `status` ∈ `Pending | Processing | Completed | Failed`. `POST` is
idempotent-ish — calling it again while one is already `Pending`/`Processing` returns
that same request rather than queuing a duplicate. The export itself (profile + the
caller's own reports + attachment metadata + notifications — never other reporters'
data) is built by a periodic background sweep, not synchronously; poll `GET
.../data-export` for status. `downloadUrl`/`downloadUrlExpiresAt` are only present once
`status` is `Completed`, and are freshly issued on every call — there is no permanent or
long-lived download link, same convention as every other signed URL in this API.
`GET .../data-export` returns `404 not_found` if the caller has never requested one.

**Account deletion**: `DELETE /` records the request only — **nothing happens to the
account immediately**. `scheduledForAt` is `Compliance:AccountDeletionGracePeriodDays`
(default 14 days) out; the reporter's session keeps working and they can
`POST .../deletion/cancel` at any point before then. Calling `DELETE /` again while one
is already `Pending` returns that same request. `POST .../deletion/cancel` returns
`409 business_rule_violation` if there's no `Pending` request to cancel. Once the grace
period elapses, a background sweep anonymizes the account (name/email/phone/password/
WhatsApp identity all scrubbed, every session and device token revoked) — the caller's
own `IncidentReport` rows are never deleted or altered, kept for audit/legal continuity.
There is no user-facing "it happened" signal beyond the account simply no longer being
able to log in — by the time it executes, nothing is left to notify.

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
