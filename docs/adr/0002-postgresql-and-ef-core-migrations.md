# ADR 0002 — PostgreSQL and EF Core Migrations

- Status: Accepted
- Date: 2026-08-22

## Context

CampusCore stores relational, integrity-sensitive data: students, guardians, enrollment, attendance, academic records, staff, settings, announcements, audit history, and identity data. Many operations need uniqueness, foreign keys, transactions, filtering, aggregation, and reliable schema evolution.

The repository uses .NET 9 and Entity Framework Core. A production database choice should match those requirements without introducing a separate persistence model for each module.

## Decision

CampusCore uses **PostgreSQL** as the primary relational database and **Entity Framework Core migrations** as the authoritative schema-history mechanism.

Rules:

- schema changes are represented by committed migrations;
- released migrations are immutable historical records;
- later corrections are made with new migrations;
- production deployments review generated SQL before applying material changes;
- multi-step state changes use transactions where atomicity is required;
- database constraints protect durable invariants in addition to application validation;
- indexes are added for measured query patterns rather than speculative completeness;
- seed data is fictional and safe to publish;
- application code uses UTC-aware timestamps and explicit `DateOnly`/`TimeOnly` concepts when wall-clock dates/times are the domain value.

## Consequences

### Positive

- PostgreSQL provides mature relational constraints, indexing, transactions, and query tooling.
- EF Core keeps persistence integration natural for the .NET application while preserving migration history in source control.
- A clean installation and an upgrade path can be verified from the same repository.
- Idempotent migration scripts can be generated for controlled deployments.

### Trade-offs

- PostgreSQL must be available for realistic integration and migration testing.
- EF Core abstractions do not remove the need to inspect generated SQL and query plans.
- Database-specific behavior means SQLite-only tests cannot prove every production persistence behavior.

## Alternatives considered

### SQLite as the production store

Rejected for the primary deployment because concurrency, operational tooling, and SQL behavior differ from the intended production workload. SQLite can still be useful for narrow tests when its differences are understood.

### Document database

Rejected because CampusCore has strongly relational data and transaction/integrity requirements.

### Manual SQL schema management without migrations

Rejected because it weakens reproducibility and makes clean install/upgrade history harder to audit.

## Review triggers

Revisit when:

- a demonstrated workload cannot be served adequately by PostgreSQL after query/index optimization;
- regulatory or deployment constraints require a different supported database;
- a module becomes independently owned and needs a separate persistence boundary;
- EF Core migration tooling no longer meets deployment or compatibility requirements.
