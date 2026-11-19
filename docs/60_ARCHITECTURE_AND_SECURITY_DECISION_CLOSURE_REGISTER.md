# 60 — Architecture and Security Decision Closure Register

- **Document ID:** GOV-DECISIONS-060
- **Status:** Current
- **Decision maturity:** Open decisions register; no new product or security policy is approved here
- **Owner:** Product owner (accountable role; named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)

## Purpose and authority

This register consolidates implementation-blocking architecture, security, data-governance and operating decisions already identified in documents 11, 42, 44, 46, 48, 51 and 57. It does not replace those specifications or ADRs, and it does not make decisions on behalf of the accountable owners.

Every item remains Open unless an approved ADR or external evidence is linked. A decision closes only when the accountable owner approves a recorded outcome, affected specifications are updated, and required validation evidence is linked.

## Decision closure register

| ID | Decision requiring closure | Current state | Accountable owner | Affected documents | Required closure evidence / gate |
|---|---|---|---|---|---|
| DEC-001 | Exact Tenant → School/Legal Entity → Campus cardinality and legal customer identity | Open | Product owner | 00, 04, 36, 42–44, 46 | Approved domain cardinality, glossary and ERD |
| DEC-002 | Primary-key strategy and external identifier policy | Open | Technical lead | 43–45 | Approved key strategy, constraints and migration convention |
| DEC-003 | Pilot topology: shared group node vs per-campus edge nodes | Open per pilot | Product owner + pilot customer | 14, 17, 18, 48 | Signed Deployment Profile Record and network topology |
| DEC-004 | RLS context, DB roles, table ownership and connection-pool behavior | Open | Technical lead + security owner | 07, 38, 44, 46 | Threat-reviewed RLS design and pooled-connection isolation tests |
| DEC-005 | Fees-to-Finance transaction boundary and posting integration | Open | Backend lead + finance owner | 02, 04, 43–45 | Approved posting contract, outbox/transaction rules and reconciliation tests |
| DEC-006 | Accounting basis, chart of accounts, fiscal year, receipt numbering and approval thresholds | Open | Finance owner | 04, 08, 43, 46, 47 | Approved accounting policy and finance acceptance suite |
| DEC-007 | Initial bank formats, payment providers and webhook evidence requirements | External verification | Integration owner + finance owner | 11, 45, 47, 50 | Provider/bank contracts, verified signatures and test evidence |
| DEC-008 | Authentication provider, MFA, local identity lifecycle and recovery model | Open | Technical lead + security owner | 00, 07, 38, 45, 46 | Approved identity architecture, threat model and security tests |
| DEC-009 | National identifier classification, child/guardian privacy, legal basis, retention and deletion | External verification | Security owner + legal counsel | 07, 38, 43, 50 | Pakistan-specific legal/privacy review and approved retention schedule |
| DEC-010 | Operation-by-operation Restricted Mode allow/deny matrix | Open | Product owner + technical lead + finance owner | 15, 46–48 | Approved operation matrix and automated authorization tests |
| DEC-011 | Cloud replication, parent portal projection and permitted data domains | Open | Product owner + security owner | 14, 18, 36, 45, 48 | Field-level projection contract, purpose, scope, freshness and retention |
| DEC-012 | Offline marks approval and provisional-vs-campus-final cash receipt semantics | Open | Product owner + academic/finance owners | 02, 05, 43, 45, 47, 57 | Explicit workflow states, approval authority and outage acceptance tests |
| DEC-013 | Demo session storage isolation, timeout values, cleanup target and backup residual lifetime | Open | Product owner + security + operations | 31, 36, 38, 41, 45, 52 | Threat/load test, deletion-window evidence and approved limits |
| DEC-014 | Curriculum/board data scope, licensing and content usage rights | External verification | Academic owner + legal counsel | 01, 04, 43, 47 | Board/content rights evidence and approved ingestion policy |
| DEC-015 | Customer data migration scope, historical depth and opening-balance method | Open per pilot | Product owner + finance/migration owner | 43, 47, 50 | Source mapping, dry-run, financial reconciliation and customer acceptance |
| DEC-016 | English/Urdu/Roman Urdu UI launch scope and RTL acceptance | Open | Product owner | 01, 49 | Approved locale scope, translation ownership and RTL test results |
| DEC-017 | Backup schedule, RPO/RTO, off-site copy, key custody and recovery ownership | Open per service tier | Operations owner + commercial owner | 07, 16, 48, 52 | Restore drills and approved customer-facing service commitments |
| DEC-018 | Final tariffs, tax/withholding, cancellation, refunds and customer exit policy | Open / external verification | Commercial owner + finance + legal | 27, 28, 36, 50 | Pilot cost evidence, tax/legal review and approved price book/contract |
| DEC-019 | Support hours, SLA, node replacement and support-access operating policy | Open | Operations owner + commercial owner + security | 16, 35, 38, 48 | Staffing model, customer contract, time-limited support-access tests |

## Closure workflow

1. Assign a named decision owner before the decision enters active review.
2. Record context, options, constraints and recommendation in the relevant ADR or controlled decision proposal.
3. Obtain explicit approval from the accountable role and any required domain/security/legal reviewer.
4. Update affected specifications, ERD/data dictionary, API/event contract, authorization matrix and acceptance tests.
5. Update requirement/task mappings and evidence links where relevant.
6. Change a row from Open only when its approved ADR or external evidence is linked.
7. Keep implementation status separate: an accepted design is not implemented until code, migrations, tests and operational evidence exist.

## Relationship to other registers

- [11 — Open Questions](11_OPEN_QUESTIONS.md) retains detailed question context.
- [51 — Requirements Traceability and Documentation Sign-off](51_REQUIREMENTS_TRACEABILITY_AND_DOCUMENTATION_SIGNOFF.md) records requirement gates.
- [Requirement traceability CSV](../registers/requirement-traceability.csv) records requirement-to-task-to-test mappings.
- [Machine-readable registers](../registers/README.md) identifies canonical task-status partitions.

Do not mark Phase 1 ready while a P0 decision remains unresolved unless the product owner and technical lead explicitly accept it as a documented implementation blocker.
