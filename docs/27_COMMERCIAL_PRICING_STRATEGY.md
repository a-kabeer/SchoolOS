# 27 — Commercial Pricing Strategy and Profitability Analysis

**Document version:** 1.0 (introduced in Blueprint v1.5)  
**Prepared:** 9 November 2026  
**Status:** Commercial analysis baseline — **not final pricing approval**  
**Currency:** Pakistani Rupees (PKR)

## 1. Purpose and decision status

This document records the pricing model discussed for SchoolOS, a hybrid school operating platform for private schools and school chains in Pakistan. It separates customer-selected preferences from numeric prices that are only hypotheses for pilot testing.

### 1.1 Confirmed preferences for the commercial model

The following choices were explicitly selected by the product owner as the direction to analyse. They are the current planning baseline, but they do not constitute final approval of the complete commercial price list.

| Topic | Current preference | Status |
|---|---|---|
| Pricing model | Hybrid: organization subscription + student capacity/overage + node charges/add-ons where applicable | Selected for analysis; commercial approval pending |
| Billing boundary | One organization-wide subscription, not a separate student subscription per campus | Selected for analysis |
| Student count | Count unique active students across all campuses in the organization | Selected for analysis |
| Local hardware | School provides and owns/operates its local server hardware | Selected for analysis; support responsibility matrix pending |
| Included local nodes | First registered Local Node included; additional active Local Nodes may carry recurring fees | Selected for analysis; fee amount pending |
| Core modules | All standard core school-management modules included across packages | Selected for analysis; final module catalogue/exception policy pending |
| Student overage | Monthly calculation using a fixed billing snapshot | Selected for analysis |
| Onboarding | Paid one-time installation/onboarding rather than universally free | Selected for analysis; amount/scope pending |
| Billing cadence | Monthly and annual subscription options | Selected for analysis |

### 1.2 User-selected numeric starting preferences — not final prices

| Item | Working preference | Status |
|---|---:|---|
| Organization base subscription | Rs. 2,500/month | Starting preference; validate with pilots |
| Included active students | 200 | Starting preference; validate with pilots |
| Additional active student | Rs. 10/student/month | Starting preference; validate with pilots |

These values are used for the examples and package illustrations below. They must not be represented as a final public tariff until commercial approval, pilot validation and cost modelling are complete.

### 1.3 Assistant-proposed hypotheses — explicitly unapproved

| Item | Illustrative proposal | Status |
|---|---:|---|
| Additional active Local Node | Rs. 1,000/node/month | Unapproved test hypothesis |
| Standard remote onboarding | Rs. 5,000 one-time | Unapproved test hypothesis |
| Annual prepayment discount | 10% | Unapproved test hypothesis |
| Additional-node setup | Separate one-time fee or quoted service | Scope and amount pending |
| On-site installation/training | Travel and service quotation | Scope and amount pending |
| Professional/Enterprise volume discounts | Potential negotiated bands | Not defined |

## 2. Proposed organization-level billing model

SchoolOS should issue one subscription to the customer organization. Billing should aggregate billable students across the organization's campuses, while access permissions and operational databases remain campus/node scoped.

**Proposed recurring invoice formula:**

\[
M = B + \max(0,S-I)\times P + \max(0,N-1)\times F
\]

Where:

- `M` = monthly recurring invoice before taxes and optional usage charges.
- `B` = organization base subscription (working preference: Rs. 2,500/month).
- `S` = organization-wide unique active student count at the billing snapshot.
- `I` = included student allowance (working preference: 200).
- `P` = per-additional-student monthly rate (working preference: Rs. 10).
- `N` = number of active, registered/licensed Local Nodes entitled to operate.
- `F` = additional-node monthly fee (Rs. 1,000 is only an unapproved test hypothesis).

The first registered Local Node is included in the base subscription. An additional physical campus does **not automatically** mean an additional billable node. A node is a separately registered Local Data Plane installation. A customer with several campuses using one shared node should not be charged for nodes it does not operate; a customer needing independent offline service at each campus will generally need a node at each such campus.

### 2.1 Student counting rules — recommended implementation policy

The following rules are recommended and still require approval before implementation:

- Count unique students with an active enrollment in the organization at the monthly snapshot time.
- A student transferred between campuses within the same organization is counted once, not once per campus.
- Applicants/inquiries, withdrawn students, graduates and archived historical records are excluded.
- Suspended-but-still-enrolled student treatment must be explicitly decided; proposed default is billable while enrollment remains active.
- Students with multiple concurrent campus/section enrollments must still have one organization-level billing identity unless the business deliberately defines otherwise.
- Preserve a timestamped snapshot and the underlying count breakdown used to calculate each invoice.
- Do not allow React/client-side counts to determine the invoice. The Cloud Control Plane billing service must calculate the authoritative server-side count from trusted, synchronized enrollment facts.
- Define what happens when a campus is disconnected and the cloud does not yet have the latest count. Use an agreed freshness rule, provisional snapshot or later reconciliation; do not silently assume stale cloud data is complete.

### 2.2 Monthly snapshot and customer experience

Recommended workflow, pending final policy:

1. Choose one consistent monthly snapshot date/time and timezone (Pakistan Standard Time is the expected default, subject to confirmation).
2. Calculate the organization's unique active students across all registered campuses.
3. Show the administrator a preview before invoice finalization (five days in advance is one possible pilot setting, not a confirmed rule).
4. Provide campus-level breakdowns and a clear explanation of transfers, exclusions and adjustments.
5. Allow a documented correction/dispute window and create auditable adjustment entries rather than editing a finalized snapshot invisibly.
6. Generate the invoice with base fee, included capacity, overage quantity/rate, active node count and node fee as separate lines.
7. If student count decreases, reduce the next applicable monthly bill; do not retroactively rewrite already-issued invoices except through an explicit credit note/adjustment policy.

## 3. Illustrative package matrix

The table below applies the starting formula. Package labels are customer-facing capacity bands, not feature gates. All standard core modules are intended to be included in every package under the current preference.

| Illustrative package band | Students at band point | Formula-derived monthly amount | Status |
|---|---:|---:|---|
| Starter | 200 | Rs. 2,500 | Based on starting preferences; validate |
| Growth | 500 | Rs. 5,500 | Formula illustration; not approved tariff |
| Professional | 1,000 | Rs. 10,500 | Formula illustration; not approved tariff |
| Enterprise | 2,000 | Rs. 20,500 | Formula illustration; not approved tariff |
| Large Network | 5,000 | Rs. 50,500 | Formula illustration; large-volume discount policy open |

The package band is a display and purchasing aid. The underlying formula should be continuous: 501 students should not trigger a sudden jump to a wholly new tier price. At 501 students, the formula yields Rs. 5,510 before node fees and add-ons.

No unlimited student capacity, campus count or Local Node count is implied. Capacity/usage and node entitlements must be explicit in the subscription and signed node licence.

## 4. Additional Local Node pricing

### 4.1 Proposed model

- First active registered Local Node: included in base organization subscription.
- Each additional active registered Local Node: recurring monthly node fee, amount pending.
- Rs. 1,000 per additional node per month is a test hypothesis only.
- New node installation, configuration and training may attract a separate one-time service charge; no amount is approved.
- Decommissioned nodes should stop recurring billing only after an authorized deactivation, data/backup handover and licence release process. Define pro-rating and mid-cycle deactivation rules.

### 4.2 Customer value included in a node fee

If a recurring fee is charged, document the ongoing value it funds, such as node registration, signed licence management, release/update distribution, sync health monitoring, standard remote diagnostics and node lifecycle administration. The fee must not be presented as a charge for a school-owned hardware asset.

### 4.3 Node vs campus distinction

`Campus` is an organizational/location concept; `Local Node` is a separately deployed software/data-plane installation. They are related but not interchangeable. One node may serve multiple connected campuses when network availability and operating policy permit. Each campus requiring independent operation while disconnected from the others needs its own Local Node. Billing should meter registered active nodes, not blindly count campus records.

## 5. One-time installation and onboarding

Paid onboarding is the current preference. Its amount and service scope remain open.

A standard remote onboarding service may include:

- Pre-installation readiness check against published hardware/network prerequisites.
- One initial Local Node installation and registration.
- ASP.NET Core application, React frontend and PostgreSQL deployment/configuration.
- Initial organization/campus setup and first administrator account.
- Backup configuration and a test restore/readiness check.
- Initial signed licence activation.
- Basic administrator orientation/training.
- Installation handover checklist and support contacts.

Legacy data migration, onsite travel, custom integrations, extensive staff training, hardware procurement, repairs and bespoke reports should be scoped and quoted separately unless expressly included in a package.

Rs. 5,000 for standard remote onboarding is an illustrative test hypothesis only. Earlier discussion also considered a wider Rs. 3,000–8,000 range depending on scope; neither amount is approved. Actual price should be based on measured onboarding hours, migration complexity, travel and support effort.

## 6. School-owned hardware and responsibility split

### School/customer responsibilities (proposed)

- Purchase and ownership of server hardware.
- Electricity, UPS, cooling and physical security.
- LAN/Wi-Fi/router/switches and connectivity between campuses.
- Operating-system administration and hardware warranty, unless a managed-appliance service is separately contracted.
- Availability of an authorized school administrator during installation and recovery.
- Protection of the physical server room and local network.

### SchoolOS responsibilities (proposed)

- Publish and maintain a supported hardware/OS compatibility matrix.
- Provide supported application packages, signed updates and documented upgrade/rollback processes.
- Manage organization subscription, Local Node registration, signed licences and entitlements.
- Provide contracted remote support, issue diagnosis and software fixes.
- Document backup responsibilities, recommended schedules, encryption and restore procedures.
- Make clear which cloud reporting/sync/remote access functions depend on internet connectivity.

The exact boundary for OS patching, database maintenance, off-site backup custody, incident response, recovery time and support hours requires a contract/SLA decision. School-owned hardware does not eliminate SchoolOS operational support costs.

## 7. Monthly and annual billing

Both monthly and annual subscription options are preferred.

### Monthly plan

- Invoice the organization on a predictable monthly cycle.
- Apply the monthly organization student snapshot and active-node count.
- Keep one invoice record with explicit line items and tax treatment.

### Annual plan

- Collect the agreed annual base subscription in advance.
- Decide whether the included student allowance and node entitlement are fixed for the year or recalculated periodically.
- A 10% discount is a test hypothesis only, not approved.
- A proposed approach is to invoice additional student overage monthly even for annual base subscribers, so growth is charged fairly; customer research must validate whether schools prefer quarterly or annual true-up instead.
- Define downgrade, cancellation, refund and mid-year capacity changes before offering annual contracts.

Illustrative annual totals at a 10% discount, excluding node fees, AI, messaging, taxes and any additional-student adjustment:

| Monthly formula amount | Annual at list price | Annual with illustrative 10% discount |
|---:|---:|---:|
| Rs. 2,500 | Rs. 30,000 | Rs. 27,000 |
| Rs. 5,500 | Rs. 66,000 | Rs. 59,400 |
| Rs. 10,500 | Rs. 126,000 | Rs. 113,400 |
| Rs. 20,500 | Rs. 246,000 | Rs. 221,400 |

## 8. Core modules and add-ons

The current preference is that all standard core modules are available in every subscription tier. Avoid making routine operations such as admissions, fees, attendance, examinations, HR, timetable and core reporting unavailable merely because a school chose a lower student band.

Candidate core catalogue (final scope still governed by Product Requirements):

- Admissions and student lifecycle.
- Fee management and finance controls.
- Attendance.
- Academics, timetable and curriculum configuration.
- Examinations, results and report cards.
- Parent and teacher portals/basic communication.
- HR and payroll (subject to agreed release scope).
- Library and inventory (subject to agreed release scope).
- Core reports and organization/campus access controls.
- Local operation, sync, backup and licence functions required by the selected deployment profile.

Variable-cost or premium services should be separately metered/quoted:

- AI Pro, advanced paper checking, voice minutes and private/local AI infrastructure.
- SMS and WhatsApp provider usage.
- Extra cloud storage or off-site backup beyond an included allowance.
- Dedicated support/SLA, onsite support and custom integrations.
- Data migration and extensive training.

The product must define a clear included AI credit allowance and usage-exhaustion behavior separately in `24_AI_PRICING_AND_USAGE_METERING.md`.

## 9. Revenue examples (not profit forecasts)

### 9.1 Organization-level recurring invoice examples

Assumptions: base Rs. 2,500/month, 200 included students, Rs. 10 per additional student/month, Rs. 1,000 per additional active node/month. The node fee is hypothetical and shown only to demonstrate the model.

| Example customer | Active students | Active nodes | Student-based fee | Additional node fee | Total monthly recurring |
|---|---:|---:|---:|---:|---:|
| Small school | 200 | 1 | Rs. 2,500 | Rs. 0 | Rs. 2,500 |
| Medium school | 500 | 1 | Rs. 5,500 | Rs. 0 | Rs. 5,500 |
| Large school group | 1,000 | 3 | Rs. 10,500 | Rs. 2,000 | Rs. 12,500 |
| Five-campus chain | 2,500 | 5 | Rs. 25,500 | Rs. 4,000 | Rs. 29,500 |
| Ten-node network | 5,000 | 10 | Rs. 50,500 | Rs. 9,000 | Rs. 59,500 |

The five-campus example assumes five independently deployed Local Nodes. If the organization uses one shared node, only the one included node is counted. Campus count alone does not determine node fees.

### 9.2 Illustrative 100-customer mix

| Segment | Customers | Students/customer | Formula monthly price/customer | Monthly revenue |
|---|---:|---:|---:|---:|
| Small | 40 | 200 | Rs. 2,500 | Rs. 100,000 |
| Medium | 30 | 500 | Rs. 5,500 | Rs. 165,000 |
| Large | 20 | 1,000 | Rs. 10,500 | Rs. 210,000 |
| Enterprise | 10 | 2,000 | Rs. 20,500 | Rs. 205,000 |
| **Total** | **100** | — | — | **Rs. 680,000** |

If this hypothetical portfolio averaged 2.5 active Local Nodes per organization and the Rs. 1,000 test fee were used, it would have 150 additional nodes and Rs. 150,000 additional monthly node revenue, giving Rs. 830,000 total monthly recurring revenue. This is a scenario, not a customer forecast or approved tariff. It excludes cancellations, discounts, collection delays, taxes, refunds, add-ons and costs.

Onboarding revenue is one-time, not MRR/ARR. At an illustrative Rs. 5,000 per 100 new customers it would be Rs. 500,000 of one-time revenue, not recurring revenue.

## 10. Profitability framework

Revenue examples must not be described as profit. Calculate contribution and business profit separately.

\[
\text{Contribution per organization} = \text{Net recurring revenue} - \text{direct variable service costs}
\]

\[
\text{Operating profit} = \text{total contribution} - \text{fixed operating expenses}
\]

Track at minimum:

- Cloud Control Plane hosting, database, monitoring, logs and bandwidth allocation.
- Sync traffic, cloud backup/object storage and retention costs.
- Licence issuance/renewal and payment processing cost.
- Customer support minutes/tickets and onsite work.
- Onboarding and data migration hours.
- Software engineering, security, QA and release maintenance.
- Sales/marketing, commissions, customer acquisition cost (CAC) and churn.
- Taxes, refunds, discounts, bad debt and payment collection delay.

Useful measures: MRR, ARR, average revenue per organization, revenue per active student, revenue per Local Node, onboarding gross margin, direct support cost per organization, contribution margin, CAC payback, churn and customer lifetime value.

A previously discussed sample cost stack (e.g. Rs. 250 cloud allocation + Rs. 500 support + Rs. 100 processing + Rs. 150 other direct costs on a Rs. 5,500 account) was purely illustrative. It is not SchoolOS cost data and must not be used for an investment forecast. Target margins should be set only after real pilot telemetry and time tracking.

## 11. Main commercial risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Student counts are stale while a campus is offline | Incorrect invoice | Snapshot freshness policy, campus breakdown, provisional status and reconciliation window |
| Duplicate students across campuses | Overbilling | Stable organization-level person/student identity and dedupe rules |
| Schools perceive node fee as an arbitrary tax | Sales friction | Explain ongoing node services and show nodes separately on invoice |
| Too many small nodes increase support load | Margin erosion | Supported node matrix, standardized deployment, node health diagnostics and paid onboarding |
| School hardware is underpowered | Poor performance/support burden | Publish hardware profiles and pre-installation readiness test |
| Onboarding work exceeds fixed fee | Negative onboarding margin | Scope limits, measured service hours, paid migration tiers and change orders |
| Annual discount erodes margin | Revenue leakage | Validate cash-flow value vs discount; exclude usage add-ons/overages unless expressly approved |
| Unlimited variable-cost AI or messaging | Unpredictable costs | Credit/usage metering, budget limits and separate add-ons |
| Student-count changes create invoice disputes | Customer dissatisfaction | Fixed snapshot, preview, clear status rules and auditable credit/debit adjustments |
| Node deactivation is used to avoid fees while node still operates | Revenue leakage/security gap | Signed node identity, lease state, decommission workflow and audit |

## 12. Pilot validation plan

Before publishing a tariff, test with a diverse group of pilot schools:

1. Interview school owners/admins about willingness to pay, preferred invoice format and perceived value of a school-owned local server.
2. Record actual installation, migration, training, support and troubleshooting time.
3. Measure cloud control-plane, backup, sync and messaging usage per organization/node.
4. Test student-count preview and invoice dispute workflows with admissions/withdrawals/transfers.
5. Compare monthly vs annual conversion and discount sensitivity.
6. Test node fees with organizations using one shared node versus multiple independent campus nodes.
7. Calculate contribution by customer segment before introducing volume discounts.
8. Revalidate competitor prices/features from official current sources before any public comparison.

Do not publish final prices based only on competitor marketing pages or hypothetical costs.

## 13. Open commercial decisions

- Final package prices and included student counts.
- Whether the Rs. 2,500 / 200 / Rs. 10 baseline survives pilot validation.
- Additional active-node monthly fee and whether it varies by node type/support tier.
- One-time onboarding amount and exact included service scope.
- Additional-node installation charge.
- Monthly billing snapshot date, preview window, dispute window and timezone.
- Suspended student billability and multi-enrollment edge cases.
- Annual discount, annual capacity rules and monthly/quarterly overage true-up.
- Treatment of taxes, withholding, refunds, credits, payment fees and overdue invoices.
- Cancellation, data export/retention, node deactivation and post-termination access.
- Whether an emergency/support-only entitlement is available after subscription restriction.
- Exact boundary between included core modules and paid services.
- Minimum local server hardware/OS profiles and who patches the operating system.

## 14. Implementation dependencies

The commercial model depends on the existing Hybrid Cloud Control Plane and Local Data Plane architecture, signed node-bound licences, organization/campus identity, reliable event-based sync and the subscription enforcement policy in `15_SUBSCRIPTION_LICENSING_AND_OFFLINE_ENFORCEMENT.md`.

Do not compute authoritative invoices from a local UI or trust a client-supplied student count. Billing and node entitlements are Cloud Control Plane responsibilities; Local Nodes cache signed entitlements and operate within their last valid licence lease while disconnected.
