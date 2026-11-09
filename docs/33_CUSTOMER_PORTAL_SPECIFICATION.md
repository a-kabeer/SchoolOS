# 33 — Customer Portal Specification

**Version:** v1.6 · **Status:** Proposed specification; Not Started

## 1. Purpose

The portal is the customer-facing management surface for the SchoolOS commercial relationship. It is not the school's daily operational application. It must not expose local student, guardian, academic or financial transaction data unless a separately approved, minimized reporting projection is explicitly designed.

## 2. Main navigation and screens

- Dashboard: subscription/licence summary, open tickets, pending actions and node status.
- Organization: profile, contacts, billing information and organization administrators.
- Campuses: registered campuses, status, location/contact and deployment profile.
- Local Nodes: node identity, version, heartbeat, last sync, licence and health summaries.
- Subscription: plan, capacity, included limits, add-ons, renewal cycle and entitlement view.
- Billing: invoices, payment references, receipts/verification status, credit notes (if applicable).
- Licences: current licence metadata, expiry/grace status, renewal instructions and signed `.lic` download.
- Downloads: approved installers, release notes, checksums and install documentation.
- Support: tickets, chat, knowledge base, contact preferences and WhatsApp channel guidance.
- Team: invite/revoke contacts, role assignment and membership history.
- Notifications: announcements, delivery preferences and event history.
- Security: MFA, sessions, login history and account recovery.

## 3. Customer roles and permissions

- Organization Owner: full organization commercial administration and customer team management.
- Organization Admin: manage organization/campus contacts and approved operational metadata.
- Finance Contact: view invoices, payment status and subscription billing; cannot issue licences or change roles unless additionally granted.
- IT Administrator: view/manage node metadata, downloads and installation support; cannot view invoices unless separately granted.
- Campus Principal: only permitted campus-specific metadata and support details.
- Support Contact: create/view permitted tickets and communicate with support.

One user may hold more than one explicit role. Role assignment must be scoped to an organization and, where relevant, campus. These portal roles do not grant SchoolOS Local Data Plane permissions.

## 4. Node and freshness display

Show last-seen timestamp, timezone, app version, licence status, latest sync summary and whether the data is current/stale/unknown. Do not label a node “offline” solely because an event is delayed without showing the last-heartbeat time and detection threshold. The customer portal depends on cloud connectivity and cannot become the authoritative local health monitor during a prolonged internet outage.

## 5. Licence UX

- Display issued date, paid-through date, grace-until date, node binding status and entitlements.
- Manual download is available only to a customer user with licence permission and must be protected against unauthorized cross-organization access.
- Licence file is signed by the isolated Cloud signing key; the portal does not generate or hold the private key.
- Show renewal status and clear actions for payment/contact/support. Never silently replace an imported active licence.

## 6. Entities

`PortalMemberships`, `PortalRoleAssignments`, `PortalInvitations`, `PortalSessions`, `PortalSecurityEvents`, `CustomerAnnouncementsReadStates`, `PortalNotificationPreferences`, `PortalDownloads`, `CustomerNodeViews` (or read models), `CustomerSupportAccessRequests`.

Use read models/projections for dashboards; do not query or join directly into campus operational databases for general portal pages.

## 7. Routes and APIs

Routes: `/portal/dashboard`, `/portal/organization`, `/portal/campuses`, `/portal/nodes`, `/portal/subscription`, `/portal/billing`, `/portal/invoices`, `/portal/licenses`, `/portal/downloads`, `/portal/support`, `/portal/team`, `/portal/notifications`, `/portal/security`.

Illustrative endpoints:
- `GET /api/v1/portal/dashboard`
- `GET/PATCH /api/v1/portal/organization`
- `GET /api/v1/portal/campuses`
- `GET /api/v1/portal/nodes`
- `GET /api/v1/portal/subscription`
- `GET /api/v1/portal/invoices`
- `GET /api/v1/portal/licenses`
- `POST /api/v1/portal/licenses/{id}/manual-download`
- `POST /api/v1/portal/support/tickets`
- `POST /api/v1/portal/team/invitations`

## 8. Acceptance criteria

- All endpoints enforce organization membership and specific permission.
- Finance Contact cannot manage nodes or customer team unless explicitly granted.
- IT Administrator cannot view financial invoices unless explicitly granted.
- Portal user cannot access a different organization's identifiers by changing route parameters.
- Node status shows timestamp/freshness and does not claim real-time connectivity during outages.
- Licence download is audited and returns only the authorized organization's signed file.
