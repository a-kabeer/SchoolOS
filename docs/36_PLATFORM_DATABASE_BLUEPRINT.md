# 36 — Business & Customer Platform Database Blueprint

**Version:** v1.7 · **Status:** Preliminary entity catalogue; detailed ERD and migrations Not Started

## 1. Database boundary

This catalogue covers the **Cloud Control Plane database only**. Campus Local Data Plane operational databases remain separate. Do not create foreign keys from the BCP database into local campus databases. Cross-plane information arrives through approved sync events or read-model projections.

Use PostgreSQL with EF Core/Npgsql. Suggested schemas are logical module ownership boundaries in one database, not a requirement to create a database server per module. Keep migrations owned by each module and avoid cross-module writes that bypass application contracts.

## 2. Suggested namespaces and entities

| Schema | Main entities | Purpose |
|---|---|---|
| `identity` | Users, UserCredentials, UserSessions, MfaFactors, Roles, Permissions, RolePermissions, UserRoleAssignments, Invitations, LoginAudits | Platform identity and authorization |
| `organizations` | Organizations, OrganizationProfiles, OrganizationContacts, Campuses, CampusContacts, LocalNodes, NodeCertificates, NodeHeartbeats, NodeHealthSnapshots, NodeSyncSummaries | Customer and node registry metadata |
| `crm` | Leads, LeadContacts, LeadActivities, LeadAssignments, LeadTasks, LeadPipelineHistory | Sales pipeline and lead management |
| `demo` | DemoTemplates, DemoTemplateVersions, DemoSessions, DemoSessionUsers, DemoSessionClients, DemoSessionResources, DemoSessionEvents, DemoEnvironments, DemoResetJobs, DemoRequests, SalesAppointments | Safe demo and appointment management |
| `registration` | RegistrationApplications, RegistrationContacts, VerificationChallenges, OrganizationReviews, ReviewDecisions, OnboardingCases, OnboardingChecklistItems, OrganizationInvitations, TrialEntitlements | Customer registration and approval |
| `subscriptions` | SubscriptionPlans, PlanPrices, PlanEntitlements, OrganizationSubscriptions, SubscriptionItems, EntitlementAssignments, SubscriptionHistory | Plans and organization-level service entitlement |
| `billing` | StudentCapacitySnapshots, NodeBillingSnapshots, Invoices, InvoiceLines, Payments, PaymentAllocations, CreditNotes, Discounts, PaymentVerificationCases | Commercial billing and verification |
| `licensing` | LicenseLeases, LicenseEntitlements, LicenseImportReceipts, LicenseRenewalEvents, LicenseRevocationEvents, SigningKeyMetadata | Signed licence lifecycle; metadata only |
| `portal` | PortalMemberships, PortalRoleAssignments, PortalInvitations, PortalSessions, PortalSecurityEvents, PortalNotificationPreferences | Customer portal membership/preferences |
| `support` | SupportTickets, TicketMessages, TicketAssignments, TicketAttachments, TicketStatusHistory, ChatSessions, ChatMessages, WhatsAppMessages, SupportAccessGrants, RemoteSupportSessions | Support channels and controlled diagnostics |
| `cms` | WebsitePages, WebsitePageRevisions, WebsiteSections, NavigationMenus, BlogArticles, FAQs, MediaAssets, SeoMetadata, RedirectRules, ContentReviews | Public content management |
| `notifications` | NotificationTemplates, NotificationDeliveries, UserNotificationPreferences, Announcements, ConsentRecords | Delivery and customer preferences |
| `reporting` | BusinessMetricSnapshots, NodeStatusReadModels, SubscriptionReadModels, AiUsageSummaries | Aggregated dashboards and read models |
| `audit` | AuditLogs, AdminActionLogs, SecurityEvents, WebhookEvents, IdempotencyRecords, AdminApprovalRequests | Audit, security and deduplication |
| `platform` | FeatureFlags, SystemConfigurations, DataRetentionPolicies, ReleaseChannels, DownloadArtifacts | Platform configuration and releases |

Names are a conceptual catalogue, not a frozen schema. Combine or split tables only through reviewed ADR/schema changes.

## 3. Common columns and data rules

For mutable records consider: `Id` (UUID), `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy`, `RowVersion`/concurrency token and `Status`. Use UTC timestamps. Financial values use fixed-precision numeric/decimal and explicit currency code. Store phone numbers in normalized international format where possible. Use stable external IDs and idempotency keys for webhooks and node events.

Organization-owned entities must carry `OrganizationId` directly or through a rigorously enforced parent relationship. Campus-specific records also carry `CampusId` where it materially simplifies authorization and query safety. Add indexes for tenant-scoped lookups, status/date queries, unique business keys and webhook idempotency. Define deletion/retention behavior explicitly; prefer status/retention workflows over destructive cascade deletion for financial, licence and audit records.

## 4. Key relationships

- One Organization has many Campuses, Contacts, LocalNodes, Subscriptions, Tickets and PortalMemberships.
- A Campus belongs to one Organization and may be served by one or more registered node identities over time; node history must preserve replacement/deactivation events.
- One Organization has a commercial subscription at a time under the initial model, with history and effective-dated changes.
- One subscription has plan/entitlement assignments and one or more invoices.
- Payments allocate to invoices through `PaymentAllocations`; payment verification is a separate workflow.
- Licence leases bind to organization + campus/node identity and include signed entitlements/sequence metadata.
- Leads may convert to an Organization; preserve lead history and conversion link.
- Demo sessions link to template versions and demo resources, never production school records.
- Support tickets belong to organization/customer contact when authenticated; anonymous pre-sales records remain separate until identity is verified.

## 5. Critical database invariants

- Unique organization/customer identifiers; unique verified email subject where applicable.
- No cross-tenant foreign key or query scope bypass.
- Unique provider webhook event/message ID within provider scope.
- Unique idempotency key per action scope.
- No duplicate active subscription where business policy disallows it.
- Invoice number uniqueness within the approved issuer/financial year scope.
- Immutable or append-only payment verification, licence event and audit history where legally/operationally required.
- Licence signing private key is never stored in PostgreSQL, config files, source control or a customer portal.
- Demo records have explicit environment/session boundary and expiry.

## 6. Security and privacy notes

- Use application-level authorization for all reads/writes; PostgreSQL RLS may provide defense in depth but is not the only authorization layer.
- Avoid storing local student records in BCP. Any reporting projection must be field-minimized, purpose-limited and tenant-scoped.
- Encrypt secrets with an appropriate secret manager; encrypt sensitive personal data at rest as required by threat model.
- Mask sensitive data in admin grids and logs.
- Define retention and deletion schedules for leads, demo sessions, chat, WhatsApp records, attachments and identity data.

## 7. Required design artifacts before migrations

- ERD with cardinalities and ownership.
- Field-level data dictionary and classification.
- Unique/index/foreign-key constraints.
- Retention and deletion schedule.
- Tenant/campus authorization matrix.
- Migration, rollback and seed-data plan.
- Threat model and data-flow diagram.


## Ephemeral demo session schema update (v1.7)

The `demo` namespace must support the session-bound lifecycle. `DemoSessions` records template/version, server timestamps, lifecycle state, idle/absolute expiry, revocation and cleanup state. `DemoSessionClients` stores opaque client lease identifiers and heartbeat/disconnect/lease-expiry timestamps. `DemoSessionResources` inventories session-owned database/object/cache/queue/runtime resources and cleanup state. `DemoSessionEvents` contains minimal lifecycle/security metadata only, not demo record payloads or credentials. All data access is derived from the authenticated server-side demo session, with database-enforced session isolation (RLS when shared tables are used). See `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and ADR-021.
