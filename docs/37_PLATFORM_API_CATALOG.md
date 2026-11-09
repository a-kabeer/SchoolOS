# 37 — Business & Customer Platform API Catalog

**Version:** v1.7 · **Status:** Illustrative API families; contracts Not Started

## 1. Conventions

Base path: `/api/v1`. Use JSON, consistent problem-details errors, pagination, request correlation IDs and UTC timestamps. Mutating endpoints use idempotency keys where retries could create duplicate effects. APIs below are proposed names, not approved OpenAPI contracts.

Use separate audience policies for public, demo, portal and admin APIs. Admin endpoints must never rely on UI hiding as authorization. The authenticated principal determines organization scope; client-provided `organizationId` is not authoritative.

## 2. Endpoint families

### Public website
- `GET /public/pages/{slug}`
- `GET /public/plans`, `/public/features`, `/public/navigation`, `/public/articles`, `/public/faqs`
- `POST /public/contact`, `/public/leads`, `/public/demo-requests`

### Registration/onboarding
- `POST /registration/start`, `/verify-email`, `/verify-phone`, `/organization`, `/submit`
- `GET /registration/status`
- Admin review endpoints under `/admin/registrations/{id}` for request-information, approve and reject.

### Demo
- `GET /demo/templates`
- `POST /demo/sessions`
- `GET /demo/sessions/current`
- `POST /demo/sessions/current/reset`
- `POST /demo/sessions/current/clients/connect`
- `POST /demo/sessions/current/heartbeat`
- `POST /demo/sessions/current/clients/disconnect` (best-effort close hint)
- `POST /demo/sessions/current/end` (immediate explicit logout/revocation)
- `POST /demo/requests`

### Customer portal
- `GET/PATCH /portal/organization`
- `GET /portal/dashboard`, `/campuses`, `/nodes`, `/subscription`, `/invoices`, `/licenses`, `/downloads`
- `POST /portal/licenses/{id}/manual-download`
- `POST /portal/team/invitations`
- `POST /portal/support/tickets`

### Super Admin
- `GET /admin/dashboard`, `/organizations`, `/organizations/{id}`, `/campuses`, `/nodes`, `/leads`, `/demos`, `/subscriptions`, `/billing`, `/payments`, `/licenses`, `/support`, `/audit-logs`
- `POST /admin/organizations/{id}/approve`, `/suspend`, `/reactivate`
- `POST /admin/payments/{id}/verify`
- `POST /admin/licenses/issue`, `/revoke`
- `POST /admin/support-access-grants`

### Support
- `POST /support/tickets`
- `GET /support/tickets`, `/support/tickets/{id}`
- `POST /support/tickets/{id}/messages`, `/assign`, `/resolve`, `/reopen`
- `POST /support/chat/sessions`
- `POST /support/access-requests`

### Webhooks
- `POST /webhooks/whatsapp/messages`
- `POST /webhooks/whatsapp/status`
- Future payment-provider and notification-provider webhooks only after provider selection.

## 3. API behavior requirements

- Use resource-based authorization for organization, campus, node, invoice, licence and ticket objects.
- Demo endpoints use a separate auth audience/cookie namespace. Derive the session from the secure credential and server-side record; never authorize from a client-provided session ID. Re-check session status/expiry on every request.
- Demo close signals are best-effort only. Server-side client leases, reconnect grace, inactivity/absolute expiry and a cleanup sweeper are mandatory. Explicit logout revokes immediately; cleanup is asynchronous, idempotent and retryable.
- For list endpoints, apply tenant/role filter before pagination and avoid leaking counts of unauthorized rows.
- Use optimistic concurrency/ETag or version fields for conflicting admin edits.
- Return stable error codes for validation, authorization, conflict, expired session, duplicate event and rate limit.
- Protect state transitions against replay and duplicate execution.
- Avoid returning sensitive node tokens, certificate private material, raw support-access secrets or provider credentials.
- Webhook endpoints verify provider signature, timestamp tolerance, schema and idempotency before enqueue/processing.

## 4. API contract deliverables

Before implementation freeze: OpenAPI document, auth audience/policy per route, request/response DTOs, validation rules, error catalog, rate limits, idempotency scope, audit requirements, pagination strategy, versioning policy and integration tests.


## Ephemeral demo API notes (v1.7)

See `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md`. Session actions use `/demo/sessions/current` and authenticated server-side scope. Avoid public APIs that accept an arbitrary `{sessionId}` as authorization. Personalized demo requests remain a separate CRM workflow and must not extend an instant demo workspace lifetime.
