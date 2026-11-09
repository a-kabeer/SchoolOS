# ADR-019 — Organization Subscription and Commercial Metering

**Date:** 9 November 2026  
**Status:** Proposed commercial direction for validation; not final pricing approval  
**Owners:** Product and commercial leads

## Context

SchoolOS supports single-campus schools and multi-campus organizations. Each independently operated campus may require its own Local Node, while subscription, organization-level reporting and student capacity are managed centrally. A campus-by-campus subscription could create confusing invoices and penalize organizations for moving students between their own branches. Conversely, unlimited Local Nodes and unlimited support could make the hybrid service unprofitable.

## Options considered

1. Fixed price per campus.
2. Pure per-student price with no base subscription.
3. One organization base subscription plus organization-wide student capacity/overage and separately metered additional Local Nodes.
4. Tiered feature packages with different core modules.

## Current selected direction for analysis

Option 3 is the current commercial planning baseline:

- One subscription per customer organization.
- Bill unique active students across all campuses in the organization.
- Monthly student-count snapshot and monthly overage calculation.
- First registered Local Node included; additional active Local Nodes may incur a recurring node fee.
- School provides/owns local server hardware.
- Paid one-time onboarding/installation.
- Monthly and annual billing options.
- All standard core modules intended to be available across capacity bands; variable-cost services such as AI credits and messaging are separate add-ons.

## Numeric starting point — not approved tariff

The owner selected these values as preferences to analyse: Rs. 2,500 monthly base, 200 included students and Rs. 10 per additional student per month. The following are assistant-proposed hypotheses only: Rs. 1,000/month per additional node, Rs. 5,000 standard remote onboarding and 10% annual discount. Package bands derived from the formula are illustrations. All numeric pricing requires customer and cost validation before approval or publication.

## Consequences

### Positive

- One organization-wide invoice is simpler for a school chain.
- Internal campus transfers do not automatically create double student charges.
- Additional independently operated nodes can contribute to recurring service revenue.
- School-owned hardware reduces SchoolOS capital expenditure and hardware inventory obligations.
- Keeping core modules in every package gives a clear and consistent product proposition.
- Paid onboarding can offset initial installation, training and migration effort.

### Risks / trade-offs

- Student counts may be stale when campus nodes are disconnected; snapshots need freshness status and reconciliation rules.
- Node count must mean separately registered Local Node installations, not merely campus count.
- Supporting school-owned hardware creates compatibility, OS maintenance and troubleshooting boundaries.
- Paid onboarding can create adoption friction if its scope/value is unclear.
- Annual discounts and monthly overage need clear contracts and invoice treatment.
- A base + overage + node formula requires a transparent invoice preview and dispute process.

## Open matters

- Final base, included capacity and overage rate.
- Additional node recurring fee and deactivation/proration.
- One-time onboarding scope and price.
- Annual discount and capacity true-up.
- Student snapshot date, suspended enrollment and offline-data reconciliation.
- Tax, refunds, overdue invoices, cancellation and retention rules.
- Minimum server specifications and support SLA.

## Acceptance criteria before commercial release

- Pilot schools validate willingness to pay and invoice clarity.
- Actual onboarding, support, sync, backup and control-plane costs are measured.
- Student count is organization-wide, unique and reproducible.
- Node billing and signed licence entitlement are consistent.
- Monthly and annual invoice/payment scenarios pass accounting tests.
- Contract terms, privacy/legal review and tax treatment are approved.
