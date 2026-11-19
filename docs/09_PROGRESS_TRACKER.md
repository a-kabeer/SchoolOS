# 09 — Progress Tracker

**Project status:** Planning / discovery. No implementation tasks are marked complete in this archive.

## Status definitions

- `Not Started`
- `In Progress`
- `Blocked`
- `In Review`
- `Completed`

## Phase-level tracker

| Phase | Name | Status | Owner | Evidence / notes |
|---|---|---|---|---|
| 0 | Product discovery and design | Not Started | Product owner + technical lead | |
| 1 | Platform and tenancy foundation | Not Started | Technical lead | |
| 2 | Admissions and student lifecycle | Not Started | Team | |
| 3 | Fees, finance and reconciliation | Not Started | Team | |
| 4 | Academics and offline workflows | Not Started | Team | |
| 5 | Examinations and assessment | Not Started | Team | |
| 6 | Portals and communication | Not Started | Team | |
| 7 | First AI release | Not Started | Team | |
| 8 | Advanced AI release | Not Started | Team | |
| 9 | Advanced school operations | Not Started | Team | |
| 10 | Pilot, hardening and launch | Not Started | Team | |

## Task tracker template

Copy this table for each phase or migrate it to GitHub Issues/Projects.

| Task ID | Phase | Module | Task | Priority | Status | Owner | Dependencies | Acceptance evidence |
|---|---|---|---|---|---|---|---|---|
| PLAT-001 | 1 | Platform | Create ASP.NET Core solution | Critical | Not Started | | | |
| PLAT-002 | 1 | Frontend | Create React + TypeScript app | High | Not Started | | PLAT-001 | |
| TEN-001 | 1 | Organization | Implement tenant/school/campus model | Critical | Not Started | | PLAT-001 | |
| SEC-001 | 1 | Identity | Implement scoped authorization | Critical | Not Started | | TEN-001 | |
| FIN-001 | 3 | Finance | Design chart of accounts and posting rules | Critical | Not Started | | Phase 0 finance rules | |
| FIN-002 | 3 | Fees | Implement invoice/payment allocation model | Critical | Not Started | | FIN-001 | |
| REC-001 | 3 | Reconciliation | Import bank statement batches | High | Not Started | | FIN-002 | |
| OFF-001 | 4 | Offline | Implement local operation queue | High | Not Started | | Phase 1 auth | |
| AI-001 | 7 | AI | Implement provider gateway | Medium | Not Started | | AI requirements | |

## Weekly work log template

| Date | Contributor | Completed | In progress | Blockers | Decisions / links |
|---|---|---|---|---|---|
| | | | | | |

## Release gate checklist

- [ ] All phase acceptance criteria met
- [ ] Tests passing in CI
- [ ] Architecture tests passing
- [ ] Tenant-isolation tests passing
- [ ] Finance invariants passing (if finance changed)
- [ ] Offline retry/conflict tests passing (if sync changed)
- [ ] Security review completed
- [ ] Documentation updated
- [ ] Migration/rollback plan reviewed
- [ ] Release notes prepared


## Hybrid deployment work packages (v1.1 additions)

All tasks below start as `Not Started`; no implementation is implied by adding this plan.

| Task ID | Work package | Task | Priority | Status | Acceptance evidence |
|---|---|---|---|---|---|
| HYB-001 | H0 | Decide shared group node vs per-campus edge topology for pilot customers | Critical | Not Started | Approved deployment design per pilot |
| HYB-002 | H1 | Build Cloud Control Plane tenant, subscription and billing foundation | Critical | Not Started | Tenant/subscription integration tests |
| HYB-003 | H1 | Build Local Node registry and activation | Critical | Not Started | Node activation and revocation tests |
| HYB-004 | H2 | Package local ASP.NET Core, React and PostgreSQL deployment | Critical | Not Started | Clean-host installation test |
| HYB-005 | H2 | Implement local authentication and local HTTPS | Critical | Not Started | Internet-disconnected login test |
| HYB-006 | H3 | Implement signed, node-bound offline license lease | Critical | Not Started | Tamper/replay/wrong-node/expiry tests |
| HYB-007 | H3 | Implement grace, restricted mode and offline renewal-file import | Critical | Not Started | Expiry/reactivation test suite |
| HYB-008 | H4 | Implement node mTLS and transactional Outbox/Inbox | Critical | Not Started | Multi-day outage and duplicate-delivery tests |
| HYB-009 | H4 | Implement event ownership and conflict review | Critical | Not Started | Multi-node conflict tests |
| HYB-010 | H5 | Implement local and encrypted off-site backups | Critical | Not Started | Replacement-hardware restore drill |
| HYB-011 | H5 | Implement signed updates and migration safeguards | High | Not Started | Invalid-signature and rollback tests |
| HYB-012 | H5 | Implement node health, sync and license monitoring | High | Not Started | Operations dashboard and alerts |
| HYB-013 | H6 | Implement multi-campus node routing and group projections | High | Not Started | Two-campus disconnected operation demo |
| HYB-014 | H7 | Complete local server, security and commercial acceptance pilot | Critical | Not Started | Signed pilot acceptance report |

## Hybrid deployment weekly log

| Date | Node / customer | Internet state | License state | Sync queue | Backup state | Issues / actions |
|---|---|---|---|---|---|---|
| | | | | | | |


## AI workstream tracker (v1.3)

See `25_AI_IMPLEMENTATION_TASK_PLAN.md` for detailed work packages and `26_AI_PROGRESS_TRACKER.md` for decisions, dependencies and evidence. All AI workstreams begin as **Not Started**; product direction being agreed does not mean implementation is complete.

| Workstream | Status |
|---|---|
| AI-0 Discovery, privacy and provider evaluation | Not Started |
| AI-1 Provider Gateway and usage foundation | Not Started |
| AI-2 Secure tool calling | Not Started |
| AI-3 RAG foundation | Not Started |
| AI-4 Auditor and Business Copilot | Not Started |
| AI-5 Learning and Examination Intelligence | Not Started |
| AI-6 Voice and Early Intervention | Not Started |
| AI-7 Private and Campus Local AI | Not Started |


## Confirmed licensing policy and implementation tasks (v1.4)

Product policy decisions are confirmed; implementation remains **Not Started**.

| Task ID | Work package | Task | Priority | Status | Acceptance evidence |
|---|---|---|---|---|---|
| LIC-001 | H3 | Implement automatic signed-lease renewal for connected Local Nodes | Critical | Not Started | Paid connected node receives and atomically activates a renewed lease |
| LIC-002 | H3 | Implement manual node-bound `.lic` import and approved USB fallback | Critical | Not Started | Paid disconnected node renews without internet or software update |
| LIC-003 | H3 | Share one validator/activation pipeline between automatic and manual renewal | Critical | Not Started | Identical signature, scope, sequence and date checks in both paths |
| LIC-004 | H3 | Implement confirmed Restricted Mode access matrix | Critical | Not Started | Read/history/export/backup/renewal/support allowed; new operational writes denied |
| LIC-005 | H3 | Implement renewal recovery and pending-operation review | High | Not Started | Reactivation does not blindly post queued operations; validated review flow |
| LIC-006 | H3 | Add licence import/renewal audit events and admin UX | High | Not Started | Actor, node, licence ID, sequence, timestamps and outcome are auditable |

**Policy note:** Seven calendar days remains a proposed grace duration pending commercial approval. No implementation is implied by recording these decisions.

## Commercial pricing and billing workstream (v1.5)

All items below are **Not Started**. Commercial preferences have been recorded; this does not mean prices are approved or billing code exists. See `27_COMMERCIAL_PRICING_STRATEGY.md` and `28_COMMERCIAL_BILLING_IMPLEMENTATION_TASK_PLAN.md`.

| Task ID | Workstream | Task | Priority | Status | Acceptance evidence |
|---|---|---|---|---|---|
| BILL-001 | BILL-0 | Finalize organization subscription boundary and contract identity | Critical | Not Started | Approved subscription/customer model |
| BILL-002 | BILL-0/BILL-2 | Approve active-student definition, transfer dedupe and suspended status | Critical | Not Started | Tested billing-rule specification |
| BILL-003 | BILL-2 | Implement server-side monthly student snapshots and campus breakdown | Critical | Not Started | Reproducible immutable snapshot tests |
| BILL-004 | BILL-2 | Implement invoice preview, correction window and audited adjustments | High | Not Started | Admin/customer acceptance tests |
| BILL-005 | BILL-3 | Implement included first node and configurable additional-node metering | Critical | Not Started | Node registry, entitlement and billing integration tests |
| BILL-006 | BILL-4 | Implement monthly and annual billing schedules | High | Not Started | Invoice/payment/renewal integration tests |
| BILL-007 | BILL-1 | Implement all-core-module entitlement policy and separately metered add-ons | High | Not Started | Plan entitlement matrix tests |
| BILL-008 | BILL-5 | Implement paid onboarding orders, scope and completion records | Medium | Not Started | Installation handover and service billing evidence |
| BILL-009 | BILL-6 | Track MRR/ARR, support/onboarding costs and contribution by customer segment | High | Not Started | Reconciled management reporting |
| BILL-010 | BILL-0 | Validate numeric pricing with pilot customers and actual cost data | Critical | Not Started | Approved commercial price book |

**Status rule:** Do not mark a billing task Completed until code, automated tests, finance/accounting review, security review, documentation and acceptance evidence are complete.


## Business & Customer Platform workstream (v1.6)

The separate BCP workstream is tracked in `docs/39_BUSINESS_CUSTOMER_PLATFORM_TASK_PLAN.md` and `docs/40_BUSINESS_CUSTOMER_PLATFORM_PROGRESS_TRACKER.md`. All BCP-0 through BCP-11 work packages and BCP-001 through BCP-041 tasks are **Not Started**. Requirements/decisions were documented; no implementation is implied. Confirmed scope: both demo modes, both registration modes, and both built-in plus WhatsApp support.


### Ephemeral demo design addition (v1.7)

Session-bound temporary demo work is now specified in `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and ADR-021. BCP-013, BCP-014 and BCP-036–BCP-041 remain Not Started; no implementation evidence has been supplied.


## Documentation workstream (v1.8)

The following documents were added on branch `docs/implementation-ready-baseline-v1-8`. Their status is **Draft / Review Pending**; this is documentation work, not implementation completion.

| Document | Scope | Status | Required approval |
|---|---|---|---|
| DOC-001 / 42 | Repository gap register and doc-first gate | Draft | Product owner + technical lead |
| DOC-002 / 43 | Operational domain data dictionary | Draft | Technical lead + domain owners |
| DOC-003 / 44 | PostgreSQL ERD and migration specification | Draft | Technical lead + security/DBA |
| DOC-004 / 45 | API and event contracts | Draft | Backend lead + security |
| DOC-005 / 46 | Roles, authorization and tenant isolation | Draft | Product owner + security |
| DOC-006 / 47 | Workflow acceptance tests | Draft | Product owner + QA |
| DOC-007 / 48 | Deployment and recovery runbook | Draft | Operations + technical lead |
| DOC-008 / 49 | UI/UX and screen catalogue | Draft | Product owner + design/QA |
| DOC-009 / 50 | Data migration and export specification | Draft | Product owner + migration/finance |
| DOC-010 / 51 | Requirements traceability and sign-off | Draft | Product owner + technical lead + QA |

**No-code rule:** Do not start Phase 1 implementation until documents 42–51 are reviewed, open decisions are closed or accepted as blockers, and document 51 is signed off. Existing implementation phase statuses remain unchanged.


## OCI staging CI/CD and automated QA workstream (v1.8)

The developer/pilot staging pipeline is documented in `docs/52_OCI_STAGING_AND_AUTOMATED_QA_RUNBOOK.md` and tracked in `docs/53_STAGING_QA_PROGRESS_TRACKER.md`. Repository audit task STQA-001 is complete; OCI provisioning, containerization, CI/CD, Playwright and optional Gemini/MCP integration remain Not Started. Login, school-creation, campus and permission regression tests remain Blocked until the corresponding application features and test interfaces are implemented. This cloud-hosted staging environment complements, but does not replace, the production hybrid Local Node operations in document 48 and the H0–H7 work packages in document 17.


## Detailed work-package plans and consolidated registers

The summary work-package entries in this tracker remain the project-level view. Detailed checklist-level H0–H7, AI-0–AI-7, BILL and BCP work is preserved in `docs/task-plans/`. Use [59 — Task Register and Traceability Guide](59_TASK_REGISTER_AND_TRACEABILITY_GUIDE.md) and the root `registers/` directory for the consolidated task register, ID crosswalk and requirement traceability. These registers do not replace this tracker or the separate STQA tracker in document 53.
