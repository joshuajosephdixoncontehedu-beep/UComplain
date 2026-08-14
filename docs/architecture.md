# Architecture

## Overview

The UComplain Admin Portal is a two-tier web application:

1. **Frontend** — a Next.js (App Router) single-purpose admin dashboard.
2. **Backend** — an ASP.NET Core Web API on .NET 9, layered as Domain / Application /
   Infrastructure / Api, backed by a Supabase-hosted PostgreSQL database via EF Core.

The frontend has no database credentials and no Supabase client. It only ever calls the
backend's REST API over HTTPS with a JWT bearer token. This keeps a single, auditable
enforcement point for authentication, authorization, validation, and audit logging.

A future WhatsApp chatbot (not part of this build) will call the same backend to create
draft incident reports, which then flow through the same verification queue that
administrators use today. See [`whatsapp-integration-plan.md`](whatsapp-integration-plan.md).

## Backend layering

```
CommunityIncidentReporting.Api             <- controllers, middleware, JWT/Swagger/DI wiring
      depends on
CommunityIncidentReporting.Application     <- DTOs, service interfaces/implementations, validators
      depends on
CommunityIncidentReporting.Domain          <- entities, enums, shared abstractions (no dependencies)

CommunityIncidentReporting.Infrastructure  <- EF Core DbContext, repositories, auth
                                               infrastructure (JWT/password hashing),
                                               external adapters
      depends on Application + Domain, referenced only by Api
```

- **Domain** has zero dependencies on other layers or frameworks beyond the BCL. It
  defines entities (`AdminUser`, `Reporter`, `IncidentCategory`, `IncidentReport`,
  `VerificationEvent`, `ReportAssignment`, `StatusHistory`, `InternalNote`, `AuditLog`)
  and enums (`AdminRole`, `VerificationStatus`, `CaseStatus`, `IncidentPriority`,
  `SourceChannel`).
- **Application** defines use-case services and their interfaces (e.g.
  `IIncidentReportService`), request/response DTOs, and FluentValidation validators. It
  depends only on Domain — it does not reference EF Core or ASP.NET Core directly.
- **Infrastructure** implements Application's interfaces against EF Core/Npgsql,
  contains the `AppDbContext`, entity configurations, JWT token generation, and BCrypt
  password hashing.
- **Api** wires everything together: controllers call Application services, DI
  registrations bind interfaces to Infrastructure implementations, and cross-cutting
  middleware (global error handling, request logging, CORS, Swagger, JWT auth) lives
  here.

## Authentication and authorization

- Administrators authenticate with email + password (BCrypt-hashed) against
  `POST /api/admin/auth/login`, receiving a short-lived JWT access token and a
  longer-lived refresh token.
- JWT claims carry the admin's id, role, and email. ASP.NET Core authorization policies
  are role-based, matching the four `AdminRole` values (`SuperAdmin`, `IncidentManager`,
  `Reviewer`, `ReadOnlyAnalyst`).
- There is no public registration endpoint. Only an authenticated `SuperAdmin` can create
  new administrator accounts, via `POST /api/admin/administrators`.

## Verification-first data flow

Incoming reports (currently seeded for development; later submitted via the WhatsApp
webhook) start with `VerificationStatus = Pending`. A report only becomes visible in the
default `/reports` operational queue once `VerificationStatus = Verified`. Reports that
are `Rejected`, `SuspectedDuplicate`, `NeedsClarification`, or `FlaggedAbuse` are kept
out of the operational queue and instead live in the `/verification` queue for human
review. Every verification decision writes a `VerificationEvent` recording who made it,
when, the previous/new state, and an optional reason — the system never renders an
automated "this report is false" judgment; that always requires a human decision.

## Audit trail

Every state-changing admin action (status changes, assignments, verification decisions,
administrator account changes, category changes, settings changes) writes an `AuditLog`
row capturing the actor, action, entity type/id, previous/new value snapshots (JSON), and
request metadata (IP, user agent).

## Frontend structure

The Next.js app uses the App Router with route groups to separate the public
`(auth)` area (login) from the authenticated `(dashboard)` area, without those groups
appearing in the URL. Data fetching and mutations go through a typed API client
(`lib/api`) and TanStack Query for caching/loading/error state. See
`frontend/src/app` for the route tree and Phase 4+ commits for the auth/session
strategy actually implemented (documented in the root README once built).

## Deployment shape (local/dev)

`docker/` contains Dockerfiles for the frontend and backend and a `docker-compose.yml`
that runs both against a Supabase Postgres instance reached over the network — Supabase
itself is not containerized locally.
