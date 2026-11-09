# 34 — Super Admin Console Specification

**Version:** v1.6 · **Status:** Proposed specification; Not Started

## 1. Internal console scope

The Super Admin Console is for SchoolOS company staff. It manages the SaaS business and Cloud Control Plane. It is not an unrestricted remote school-management dashboard and must not grant employees blanket access to customer student/fee/exam data.

## 2. Main sections

1. Overview dashboard.
2. Organizations and Organization 360.
3. Campuses and Local Nodes.
4. Leads and CRM.
5. Demo sessions and requests.
6. Registration review and onboarding.
7. Subscription plans and entitlements.
8. Invoices, payments and credit notes.
9. Licence issuance, renewal and revocation.
10. Support tickets, chat and support access grants.
11. Knowledge Base and CMS.
12. AI usage, quota and provider cost summaries.
13. Notifications and announcements.
14. Business/operations reports.
15. Staff, roles and permissions.
16. Audit logs, security events and platform settings.

## 3. Organization directory

Search/filter by organization name, organization/customer ID, city, province, plan, subscription status, student capacity snapshot, campus count, node count, payment state, licence state, last heartbeat and last successful sync. Support sorting, pagination, saved filters and CSV export only for permitted low-risk metadata. Export must be audited.

## 4. Organization 360 page

Tabs: Overview, Contacts, Campuses, Nodes, Registration/Onboarding, Subscription, Billing, Payments, Licences, Support, Announcements, Audit History. Each tab displays only Cloud Control Plane metadata and approved summaries. Any customer-data exception requires a separate support grant.

## 5. Admin action controls

- Approve/reject registration with reason and customer-visible message.
- Verify payment with evidence reference and reconciliation note.
- Change plan/entitlement with effective date and audit.
- Register/deactivate/replace Local Node with identity verification.
- Issue/reissue/revoke signed licence according to policy and key custody controls.
- Assign/escalate/resolve support tickets.
- Publish content and customer announcements.
- Grant support access only after customer approval, restricted scope and expiry.
- Suspend/reactivate organization through a controlled workflow with impact preview.

High-impact actions may require step-up MFA or dual approval, to be defined by risk category.

## 6. Staff roles

Super Admin, Operations Manager, Sales Manager, Sales Executive, Finance Manager, Billing Officer, Licence Manager, Support Manager, Support Agent, Technical Engineer, Content Manager and Auditor (read-only). Roles are a starting catalogue; granular permissions and separation-of-duties matrix must be approved before implementation.

## 7. Entities

`StaffProfiles`, `StaffRoleAssignments`, `StaffPermissionOverrides` (avoid except justified), `AdminActionLogs`, `AdminApprovalRequests`, `AdminApprovalDecisions`, `OrganizationStatusHistory`, `PaymentVerificationCases`, `LicenseAdminActions`, `AdminSavedViews`, `AdminExportJobs`.

## 8. Routes and APIs

Routes: `/admin/dashboard`, `/admin/organizations`, `/admin/organizations/{id}`, `/admin/campuses`, `/admin/nodes`, `/admin/leads`, `/admin/demos`, `/admin/registrations`, `/admin/onboarding`, `/admin/subscriptions`, `/admin/billing`, `/admin/payments`, `/admin/licenses`, `/admin/support`, `/admin/knowledge-base`, `/admin/cms`, `/admin/ai-usage`, `/admin/reports`, `/admin/staff`, `/admin/roles`, `/admin/audit`, `/admin/settings`.

Illustrative APIs:
- `GET /api/v1/admin/dashboard`
- `GET /api/v1/admin/organizations`
- `GET /api/v1/admin/organizations/{id}`
- `POST /api/v1/admin/organizations/{id}/approve`
- `POST /api/v1/admin/organizations/{id}/suspend`
- `GET /api/v1/admin/nodes`
- `POST /api/v1/admin/payments/{id}/verify`
- `POST /api/v1/admin/licenses/issue`
- `POST /api/v1/admin/support-access-grants`
- `GET /api/v1/admin/audit-logs`

## 9. Audit requirements

Record actor, role, action, target resource, organization scope, timestamp, source IP/device context where proportionate, reason, before/after field summary and approval chain. Do not place passwords, tokens, private keys or raw student data in audit payloads.

## 10. Acceptance criteria

- Admin frontend uses separate privileged auth/session and MFA.
- Every route/API uses least-privilege authorization.
- A Sales Executive cannot verify payments or issue licences unless explicitly authorized.
- Finance staff cannot alter licence-signing keys.
- Admin organization pages do not reveal raw student/fee records by default.
- High-impact operations are previewed, confirmed, logged and reversible where business rules allow.
