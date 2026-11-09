# 28 — Commercial Billing and Pricing Implementation Task Plan

**Status:** Planning only — all implementation tasks are Not Started.  
**Depends on:** Hybrid Cloud Control Plane, organization/campus/node registry, signed licence leases, synchronization contracts and subscription enforcement.

## Workstream BILL-0 — Commercial rules and customer validation

- [ ] BILL-001: Finalize organization subscription boundary and legal customer/tenant identity.
- [ ] BILL-002: Approve billable active-student definition, transfer/deduplication and suspended-enrollment rules.
- [ ] BILL-003: Approve billing snapshot date/time, preview and dispute/correction windows.
- [ ] BILL-004: Approve monthly/annual billing, annual discount, overage true-up and downgrade/cancellation policy.
- [ ] BILL-005: Approve additional Local Node recurring fee, deactivation/pro-rating and node setup charges.
- [ ] BILL-006: Approve onboarding service catalogue, included scope, pricing and change-order rules.
- [ ] BILL-007: Approve core module inclusion matrix and paid add-on boundaries.
- [ ] BILL-008: Validate pricing, willingness to pay and direct costs through pilot interviews and telemetry.

**Exit criteria:** Approved commercial policy, actual measured service cost baseline, and customer-tested package descriptions.

## Workstream BILL-1 — Subscription catalogue and entitlements

- [ ] BILL-101: Implement versioned subscription plans, price versions, billing currency and effective dates.
- [ ] BILL-102: Implement one organization-level subscription with monthly and annual terms.
- [ ] BILL-103: Implement organization-wide student capacity and overage rules.
- [ ] BILL-104: Implement first included Local Node plus configurable additional-node entitlements/fees.
- [ ] BILL-105: Implement all-core-module entitlements across tiers; separate optional add-ons and usage quotas.
- [ ] BILL-106: Ensure historical invoices retain the exact price/version/quantity used when issued.

**Exit criteria:** Plan and entitlement tests pass; a price change does not rewrite old invoices or existing contract history.

## Workstream BILL-2 — Student metering and billing snapshots

- [ ] BILL-201: Define stable organization-wide student identity across campus transfers and merges.
- [ ] BILL-202: Implement server-side active-enrollment count across synchronized campus facts.
- [ ] BILL-203: Implement immutable monthly capacity snapshots with campus breakdown and data-freshness metadata.
- [ ] BILL-204: Implement preview, correction/dispute workflow and auditable adjustments/credit notes.
- [ ] BILL-205: Handle late offline-campus sync without silently changing a finalized invoice.
- [ ] BILL-206: Test withdrawals, graduates, suspended students, concurrent enrollments and campus transfers.

**Exit criteria:** Deterministic billable count, reproducible invoice snapshots and no duplicate billing during campus transfers.

## Workstream BILL-3 — Local Node metering

- [ ] BILL-301: Define billable node state (registered, active, suspended, decommissioning, decommissioned).
- [ ] BILL-302: Link organization subscription entitlements to registered Local Node identities.
- [ ] BILL-303: Apply first-node inclusion and additional-node fee/quantity rules.
- [ ] BILL-304: Implement authorized node deactivation, licence release, data/backup handover and billing stop date.
- [ ] BILL-305: Add audit trail for node registration, transfer, replacement and decommission.

**Exit criteria:** Campus count is not mistaken for node count; node billing and signed licence entitlements remain consistent.

## Workstream BILL-4 — Invoices, payments and annual billing

- [ ] BILL-401: Generate itemized invoices for base subscription, student overage, node charges and add-ons.
- [ ] BILL-402: Support monthly and annual subscription schedules.
- [ ] BILL-403: Implement configurable annual discount only after commercial approval.
- [ ] BILL-404: Support monthly/quarterly/annual overage true-up according to approved contract.
- [ ] BILL-405: Implement payment verification, receipts, credit notes, refunds and payment allocation.
- [ ] BILL-406: Implement taxes/withholding configuration after Pakistan accounting/tax review.
- [ ] BILL-407: Implement overdue reminders and subscription state transitions.

**Exit criteria:** Invoice and payment lifecycle is auditable, repeatable and reconciles with subscription and licence state.

## Workstream BILL-5 — Paid onboarding and service delivery

- [ ] BILL-501: Define standard remote onboarding checklist and scope.
- [ ] BILL-502: Define paid data migration, onsite installation and training service catalogues.
- [ ] BILL-503: Record onboarding orders, scope, assigned technician, hours, completion and customer acceptance.
- [ ] BILL-504: Verify school hardware/OS/network readiness before installation.
- [ ] BILL-505: Track onboarding cost and margin by organization.

**Exit criteria:** Installation scope is standardized, chargeable work is documented and setup does not silently become unlimited free support.

## Workstream BILL-6 — Reporting, controls and acceptance

- [ ] BILL-601: Build internal MRR/ARR, average revenue per organization, per-student revenue and per-node revenue reporting.
- [ ] BILL-602: Track direct support, onboarding, cloud, storage, sync and payment cost allocations.
- [ ] BILL-603: Track contribution margin, CAC, churn, refunds, discounts and collection delay.
- [ ] BILL-604: Implement billing permissions, audit logs, separation of duties and approval controls.
- [ ] BILL-605: Complete customer pilot acceptance, finance reconciliation and security review.

**Exit criteria:** Management can assess contribution by customer segment and approve a public tariff using real pilot data.

## Release rule

Do not release customer billing or auto-restrict customer operations based on unvalidated student counts, stale campus data or unapproved commercial price rules. Core school data must remain preserved under subscription expiry and the signed-licence Restricted Mode policy.
