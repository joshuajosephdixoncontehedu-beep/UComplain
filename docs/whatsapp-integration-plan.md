# WhatsApp integration plan

**Status: not implemented.** This document is a contract and design plan for a future
webhook integration — no chatbot code, message templates, or conversational logic exist
in this repository. It exists so the schema and API decisions already made (see
[`architecture.md`](architecture.md) and [`api-contract.md`](api-contract.md)) are
demonstrably compatible with the eventual integration, and so whoever builds it next
has a concrete starting contract instead of a blank page.

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
POST /api/webhooks/whatsapp   (new — not yet built)
        │  resolve Reporter, create/continue a draft IncidentReport
        ▼
PostgreSQL — same tables the admin portal already reads
        │
        ▼
Existing /verification queue → existing /reports queue → existing admin workflows
```

Nothing downstream of the database write is new. The webhook handler is the only piece
that doesn't exist yet.

## New endpoint

`POST /api/webhooks/whatsapp` — a new controller outside the `api/admin/**` tree
(`WebhooksController`, or a dedicated `CommunityIncidentReporting.Api.Controllers.Webhooks`
namespace), since it is machine-to-machine and must **not** require an admin JWT. It
needs its own authentication:

- **Signature verification**: WhatsApp Cloud API signs every webhook payload with
  `X-Hub-Signature-256`, an HMAC-SHA256 of the raw body keyed by the Meta App Secret.
  The handler must verify this before touching the body — reject with `401` on
  mismatch, the same way `AuthController` rejects bad credentials today.
- **Verification handshake**: Meta's `GET /api/webhooks/whatsapp` challenge, which must
  echo back a `hub.challenge` query parameter after checking `hub.verify_token` against
  a configured secret. This is a one-time setup step, not per-message traffic.

Both secrets (`WhatsApp__AppSecret`, `WhatsApp__VerifyToken`) would follow the existing
`backend/.env.example` convention — never hardcoded, read from configuration the same
way `Jwt__Secret` is today.

## Mapping an inbound message to the data model

| WhatsApp Cloud API field | Maps to | Notes |
| --- | --- | --- |
| `messages[].from` (E.164 phone number) | `Reporter.WhatsAppNumberHash` | Hashed the same way the seeder documents (`Reporter.cs`: "we never store the raw WhatsApp number") — never persisted in plaintext, only the hash used for lookup/dedup. |
| `messages[].from` (masked) | `Reporter.MaskedContactReference` | Same masking shown in the admin UI today, e.g. `+232 76 *** 123`. |
| First contact only | `Reporter.ConsentAt` | Set once, on the reporter's first accepted consent prompt — see [Consent](#consent) below. Never overwritten on subsequent messages. |
| `messages[].id` | Idempotency key (see below) | Not persisted on `IncidentReport` directly; used to detect webhook retries before creating a row. |
| Conversation content | `IncidentReport.Description` | Built from the conversation, not a single message — see [Conversation shape](#conversation-shape). |
| Conversation content | `IncidentReport.CategoryId`, `LocationDescription`, `IncidentOccurredAt` | Collected via the bot's own conversational flow (out of scope here) before the report is submitted to this endpoint. |
| `messages[].image` / `.document` / `.video` | `IncidentReport.MediaReference` | Store the WhatsApp **media ID**, not a downloaded copy — Cloud API media URLs expire, so a real implementation needs a background job to fetch and persist media (to Supabase Storage or similar) before the WhatsApp-hosted copy expires. That job is out of scope for this plan. |
| (fixed) | `IncidentReport.SourceChannel` | Always `SourceChannel.WhatsApp`. |
| (fixed) | `IncidentReport.VerificationStatus` | Always starts at `Pending` — a WhatsApp report gets no special trust over one entered any other way. |
| (fixed) | `IncidentReport.CaseStatus` | Always starts at `VerificationPending`, exactly like the current seed data represents unverified reports. |

## Consent

`Reporter.ConsentAt` already exists in the schema and is shown in the admin `/users`
detail page today. Before a first-time sender's message is turned into an
`IncidentReport`, the bot must present a consent notice (what's collected, how it's
used, that admins will see it) and only proceed on an affirmative reply. A sender who
never consents can still be greeted, but no `IncidentReport` row should be created for
them — this is a policy decision for the conversational flow to enforce, not something
the webhook endpoint can infer from a single inbound message.

## Idempotency

WhatsApp Cloud API retries webhook deliveries on anything other than a fast `200`. The
handler must be idempotent per `messages[].id`: on receiving a message ID already
processed, return `200` immediately without creating a second report or re-running any
side effect. A small `WhatsAppMessageId` lookup table (or a unique index if message IDs
are cheap to retain) is the straightforward way to implement this — deliberately not
added to the schema now, since it has no purpose until the webhook exists.

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

## Rate limiting and abuse

`Reporter.IsRestricted` already exists and is enforced today by admin action (see
`/users/[id]`'s restrict/unrestrict flow). A restricted reporter's future WhatsApp
messages should still be accepted (so as not to signal to a bad-faith sender that
they've been flagged) but the resulting reports should be created with elevated
scrutiny — e.g. forced into `NeedsClarification` or flagged for priority review rather
than `Pending` — a decision for the webhook handler to make at write time, not a schema
change.

## What's explicitly out of scope (here and for now)

- The conversational bot itself (message templates, multi-turn logic, menu/list
  interactions, language selection).
- Media download/storage — only the WhatsApp media ID is planned to be captured.
- A session/conversation-state store.
- Outbound notifications to reporters (e.g. "your report was verified") — the
  `SystemSettings` notification flags (`notifyOnNewVerifiedReport`,
  `notifyOnCriticalPriority`) currently only describe *admin* notifications; a
  reporter-facing notification flow is a separate, later decision.
- Any change to the existing verification, assignment, or status-transition logic —
  none is needed; WhatsApp reports flow through the exact same pipeline every other
  report already does.
