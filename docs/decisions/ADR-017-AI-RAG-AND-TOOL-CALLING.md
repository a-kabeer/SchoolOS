# ADR-017 — Permission-Filtered RAG and Allow-Listed Tool Calling

- **Document ID:** ADR-017
- **Status:** Accepted
- **Decision maturity:** Accepted direction
- **Owner:** Technical lead (accountable role; named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
- **Date:** 9 November 2026

## Context

AI needs both school-document knowledge and current operational data. A language model must not receive unrestricted database access or bypass existing module permissions.

## Decision

Use RAG for approved documents and typed allow-listed tools for operational queries/actions. PostgreSQL with pgvector is the initial vector-store candidate. Local and cloud indexes remain separate trust boundaries. Tool calls execute through authorized ASP.NET Core application services; read-only tools precede draft/action tools. Human approval is mandatory for consequential actions.

## Consequences

- Tenant/campus/ACL filtering occurs before retrieval results reach the model.
- Citations, document versions and source references are captured.
- No raw SQL, direct database credentials or arbitrary model-generated code execution.
- AI cannot directly post finance entries or publish official marks.
- Index invalidation, deletion and permission changes are part of the RAG design.

## Follow-up

Validate pgvector, document extraction/OCR, Urdu retrieval quality, tool schemas, authorization tests and local/cloud index lifecycle.
