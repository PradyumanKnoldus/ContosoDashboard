<!--
Sync Impact Report
Version change: none -> 1.0.0
Modified principles: scaffold placeholders -> five ContosoDashboard principles
Added sections: Security and Training Constraints; Development Workflow and Quality Gates
Removed sections: none
Follow-up TODOs: Confirm the original ratification date.
Resolver note: resolve-template.ps1 could not resolve its missing Resolve-TemplateContent function.
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope
ContosoDashboard MUST remain an offline, fictional training application. Changes MUST
preserve the documented non-production scope, avoid introducing required external services,
and keep production migration guidance separate from training behavior. This keeps the
repository reliable for learners and prevents training shortcuts from being mistaken for
production safeguards.

### II. Layered Architecture and Abstractions
Features MUST keep presentation, business services, data access, and domain models separated.
Infrastructure dependencies MUST be accessed through interfaces when a local implementation
may later be replaced by a cloud or production implementation. Business logic MUST NOT depend
directly on replaceable infrastructure details. This preserves the repository's offline-first
design and its migration path.

### III. Security by Default
Protected pages and operations MUST enforce authentication and authorization at the page,
service, or data boundary appropriate to the operation. User-controlled identifiers MUST be
authorized against the current user's permitted data before access or mutation. New file
handling MUST use unique stored paths and validate inputs. The mock identity system MUST be
described as training-only, not treated as production authentication. These rules provide
defense in depth and prevent IDOR-style access in the training exercises.

### IV. Verifiable Changes
Every feature or behavior change MUST include the narrowest practical automated test or
repeatable verification step. Changes to service contracts, authorization, persistence, or
cross-page behavior MUST include integration coverage or an equivalent end-to-end check.
Validation MUST cover both the intended success path and relevant unauthorized or invalid
inputs. This makes examples dependable while keeping the training workflow practical.

### V. Simplicity and Observability
Implementations MUST use the smallest design that satisfies the requirement and MUST avoid
speculative abstractions. User-visible failures and operationally meaningful events MUST be
diagnosable through clear validation, structured application logging where applicable, and
actionable error handling. New complexity MUST be justified in the change description or
review. This keeps the educational code readable without hiding important behavior.

## Security and Training Constraints

The application MUST remain suitable for local, offline instruction. It MUST NOT claim that
mock authentication, seeded credentials, local storage, or development configuration meets
production security requirements. Documentation for security-sensitive features MUST identify
the production replacement or additional controls, including a real identity provider,
password protection, MFA, TLS, audit logging, and applicable accessibility or compliance
requirements.

## Development Workflow and Quality Gates

Work MUST begin with a focused specification or issue that identifies affected behavior and
acceptance criteria. Implementation changes MUST preserve existing public behavior unless a
breaking change is explicitly documented. Before review, contributors MUST run the narrowest
relevant tests and build or type checks, inspect authorization-sensitive paths, and update
stakeholder or feature documentation when behavior changes. Reviews MUST verify compliance
with this constitution and MUST reject unexplained complexity or unverified security changes.

## Governance

This constitution governs repository design and development decisions; feature specifications,
plans, and code MUST comply with it. Amendments MUST state the affected principles, rationale,
impact on existing work, and any migration or follow-up actions. The amendment MUST be
reviewed with the same quality gates as code and recorded in the constitution's sync impact
report until the change is accepted.

Versioning follows semantic versioning: MAJOR for incompatible governance changes or removed
principles, MINOR for new or materially expanded principles or sections, and PATCH for
clarifications that do not change obligations. Compliance MUST be checked during feature
planning and review, with unresolved deviations documented and explicitly accepted by the
maintainer. The constitution MUST be revisited when the technology stack, security model, or
training scope changes.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm original adoption date | **Last Amended**: 2026-09-14
