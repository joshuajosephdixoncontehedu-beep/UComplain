# WhatsApp integration plan

**Status: the webhook receiver is implemented; the conversational bot is not.** A real
`POST /api/webhooks/whatsapp` exists and, for every inbound text message, creates one
Pending `IncidentReport` — the "minimal" version described below, verified end-to-end
against both an integration-test double and the live Supabase database. What's still
genuinely not built is any *conversation*: no multi-turn question flow, no category or
location collection, no consent prompt, no media handling. Those sections below are
still a plan, not a description of running code — each says so explicitly.

## Why this shape

The admin portal's data model was built verification-first: every `IncidentReport`
carries a `VerificationStatus` that starts at `Pending`, and the operational `/reports`
queue only ever shows `Verified` reports. A WhatsApp-submitted report is exactly the
same kind of row as one entered any other way — it just arrives through a different
`SourceChannel` and starts life with less information. The integration's entire job is
to create `Reporter` and `IncidentReport` rows correctly and then get out of the way;
verification, triage, and everything after that already exists and needs no changes.

## High-level flow

```
Community member (WhatsApp)
        │  sends a message
        ▼
Meta WhatsApp Cloud API
        │  HTTPS POST, webhook event
        ▼
POST /api/webhooks/whatsapp   (implemented)
        │  resolve/create Reporter, create one Pending IncidentReport
        ▼
PostgreSQL — same tables the admin portal already reads
        │
        ▼
Existing /verification queue → existing /reports queue → existing admin workflows
```

Nothing downstream of the database write is new or was ever expected to be — the whole
point of this shape is that a WhatsApp report is indistinguishable from any other once
it's a row in `incident_reports`.

## The endpoint (implemented)

`POST /api/webhooks/whatsapp` — `WhatsAppWebhookController`, in
`CommunityIncidentReporting.Api.Controllers.Webhooks`, deliberately outside the
`api/admin/**` tree and carrying no `[Authorize]` — there's no admin session to present
a JWT for, since Meta is the caller. It authenticates itself instead:

- **Signature verification**: WhatsApp Cloud API signs every webhook payload with
  `X-Hub-Signature-256`, an HMAC-SHA256 of the raw body keyed by the Meta App Secret.
  `WhatsAppWebhookService.VerifySignature` checks this — using a constant-time
  comparison, and reading the request body manually rather than via `[FromBody]` so the
  signature is computed over the exact bytes Meta sent, not a reserialized copy — and
  the controller returns `401` on any mismatch, including a missing header or an
  unconfigured `AppSecret` (fails closed).
- **Verification handshake**: `GET /api/webhooks/whatsapp` echoes back `hub.challenge`
  as plain text once `hub.mode=subscribe` and `hub.verify_token` match the configured
  value; anything else gets `403`. This is the one-time step Meta runs when you save the
  webhook URL in its dashboard, not per-message traffic.

Both secrets (`WhatsApp__AppSecret`, `WhatsApp__VerifyToken`) follow the existing
`backend/.env.example` convention — never hardcoded, read from configuration the same
way `Jwt__Secret` is.

## Mapping an inbound message to the data model (implemented, v1 shape)

| WhatsApp Cloud API field | Maps to | Notes |
| --- | --- | --- |
| `messages[].from` (E.164 phone number) | `Reporter.WhatsAppNumberHash` | HMAC-SHA256 keyed by `WhatsApp__NumberHashKey` — a plain unsalted hash would be reversible by brute force given how small the phone-number keyspace is, which would defeat `Reporter.cs`'s "we never store the raw WhatsApp number" claim. Never persisted in plaintext. |
| `messages[].from` (masked) | `Reporter.MaskedContactReference` | Same masked style shown elsewhere in the admin UI, e.g. `+232761 11 *** 999` — a best-effort format, not tuned per country calling-code length. |
| `messages[].id` | Audit log `newValueJson.WhatsAppMessageId` | Recorded for traceability but **not** used for deduplication yet — see Idempotency below, which is honestly a known gap in this version, not a solved problem. |
| `messages[].text.body` | `IncidentReport.Description` | The raw message text, truncated to 4000 characters. **Not** built from a multi-turn conversation — see [Conversation shape](#conversation-shape), which is still just a plan. |
| — (v1 has no category/location collection) | `IncidentReport.CategoryId` | Every WhatsApp report lands in an auto-created "Uncategorized (WhatsApp)" category (Medium priority, 48h SLA) until an admin reclassifies it from the portal — which already has full category-editing UI, so this isn't a dead end, just a deferred step. |
| — | `IncidentReport.LocationDescription` | Fixed placeholder text ("Not provided — submitted via WhatsApp, awaiting admin follow-up.") — no location parsing exists yet. |
| `messages[].timestamp` | `IncidentReport.IncidentOccurredAt` | Meta's Unix-seconds timestamp for when the message was sent; falls back to server-received time if unparseable. |
| `messages[].image` / `.document` / `.video` | *(not handled)* | Only `type: "text"` messages create a report in this version; anything else is logged and skipped, replying with nothing. Still out of scope — see below. |
| (fixed) | `IncidentReport.SourceChannel` | Always `SourceChannel.WhatsApp`. |
| (fixed) | `IncidentReport.VerificationStatus` | `Pending` normally; `FlaggedAbuse` if the sender's `Reporter` row is already `IsRestricted` (see Rate limiting below) — either way, no special trust over a report entered any other way. |
| (fixed) | `IncidentReport.CaseStatus` | Always starts at `VerificationPending`. |

## Consent — still just a plan, not implemented

`Reporter.ConsentAt` exists in the schema and is shown in the admin `/users` detail page,
but the webhook **does not set it** — v1 creates the `Reporter` row without ever
presenting a consent notice, since doing that properly needs the multi-turn
conversation this version deliberately doesn't have (see below). `ConsentAt` stays
`null` for every WhatsApp-created reporter until this is built. Worth fixing before any
real public rollout, not before a demo/dev environment.

## Idempotency — known gap, not solved

WhatsApp Cloud API retries webhook deliveries on anything other than a fast `200`. The
current handler is **not** idempotent per `messages[].id` — a retried delivery would
create a second `IncidentReport` from the same message. In practice this is a
low-probability edge case (Meta only retries on failure/timeout, not on every success),
and when it does happen the existing human-verification workflow already has a tool for
exactly this — an admin marks the duplicate as `SuspectedDuplicate` from the
verification queue, the same as any other duplicate report. A small
`WhatsAppMessageId` lookup table (or a unique index, since message IDs are cheap to
retain) would close this properly; deliberately not added yet since it's a schema
change and this fallback is good enough for now.

## Conversation shape

A single WhatsApp message rarely contains everything an `IncidentReport` needs
(category, location, description, timing). The realistic shape is a short guided
exchange — the bot asks 3–5 questions, holds partial state per sender between messages,
and only calls into report-creation once the conversation is complete. That
in-progress state is conversation/session state, not domain data, and does not belong
in `IncidentReport` (which should only ever represent a submitted report). A future
implementation will need its own lightweight session store (e.g. Redis, or a
short-TTL table) for in-flight conversations — also deliberately not part of this
plan's schema, since it's infrastructure for the chatbot itself, not the reporting
system's data model.

## Rate limiting and abuse (implemented)

`Reporter.IsRestricted` is enforced today by admin action (see `/users/[id]`'s
restrict/unrestrict flow) — and the webhook honors it: a restricted reporter's
messages are still accepted (so as not to signal to a bad-faith sender that they've
been flagged) rather than silently dropped, but the resulting report is created as
`VerificationStatus.FlaggedAbuse`/`Priority.Low` instead of the default
`Pending`/category-priority path, putting it under closer review from the start.

## Outbound acknowledgment (implemented, best-effort)

After creating a report, the webhook sends a plain-text WhatsApp reply via the Cloud
API (`POST /{phone-number-id}/messages`) confirming receipt and including the case
reference. This is intentionally best-effort: if `WhatsApp__AccessToken`/
`WhatsApp__PhoneNumberId` aren't configured, or the Graph API call fails for any
reason, it's logged and swallowed — the report is already saved by that point, and
Meta only cares that the webhook call itself returned `200`, not that a reply went
out. This is a fixed acknowledgment message, not a real notification system — see the
outbound-notifications item below for what's still genuinely out of scope.

## What's explicitly still out of scope

- The conversational bot itself (message templates, multi-turn logic, menu/list
  interactions, language selection, category/location collection, consent).
- Message deduplication by `messages[].id` — see [Idempotency](#idempotency--known-gap-not-solved)
  above.
- Media download/storage — non-text messages are currently skipped entirely, not just
  deferred; only the WhatsApp media ID was ever planned to be captured, and that
  capture isn't built either.
- A session/conversation-state store.
- Real outbound notifications to reporters (e.g. "your report was verified") — the
  `SystemSettings` notification flags (`notifyOnNewVerifiedReport`,
  `notifyOnCriticalPriority`) still only describe *admin* notifications; a
  reporter-facing notification flow beyond the fixed inbound acknowledgment above is a
  separate, later decision.
- Any change to the existing verification, assignment, or status-transition logic —
  none was needed; WhatsApp reports flow through the exact same pipeline every other
  report already does.
