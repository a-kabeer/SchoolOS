# ADR-018 — Customer Data Is Not Used for Shared Model Training by Default

- **Document ID:** ADR-018
- **Status:** Accepted
- **Decision maturity:** Accepted direction
- **Owner:** Technical lead (accountable role; named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
- **Date:** 9 November 2026

## Context

SchoolOS handles student, guardian, staff, financial and assessment information. Customers need clear boundaries around model training, retrieval, evaluation and retention.

## Decision

Do not automatically use school/customer data to train shared foundation models. RAG is retrieval, not training. Fine-tuning or customer-data training requires explicit written opt-in, data authority review, minimization/de-identification where appropriate, reviewed labels, versioned datasets and evaluation. Teacher feedback is not automatically a training dataset.

## Consequences

- Training, evaluation and feedback datasets must be distinct.
- Cross-school training is off by default.
- Provider retention and abuse-monitoring terms still require review even when training is disabled.
- Human reviewers remain accountable for high-impact outcomes.

## Follow-up

Complete data governance, customer terms, retention/deletion workflow and fine-tuning approval process with legal/privacy review.
