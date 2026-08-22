# CampusCore Performance Guide

CampusCore should remain responsive with realistic school-sized data sets without sacrificing correctness, privacy, or maintainability. Performance work must be evidence-driven.

## Performance principles

1. Measure before optimizing.
2. Protect query bounds at API boundaries.
3. Prefer server-side filtering, projection, sorting, and pagination for large collections.
4. Avoid repeated round trips and N+1 query patterns.
5. Keep browser bundles and route work proportional to what a page actually needs.
6. Cache only when ownership, scope, staleness, and invalidation rules are explicit.
7. Never weaken authorization, validation, or audit guarantees for speed.

## Initial budgets

These are engineering targets for a healthy local or staging environment, not contractual SLAs.

### API

For ordinary indexed requests with warm application/database processes:

- health endpoint: p95 under 100 ms;
- paginated student/search reads: p95 under 500 ms;
- dashboard aggregate request: p95 under 750 ms;
- common create/update operations: p95 under 750 ms excluding external storage/network delays;
- no list endpoint may return an unbounded collection.

Measure server duration separately from network/browser latency.

### Web/PWA

For a production build on a representative modern device/network:

- avoid a monolithic initial JavaScript bundle as the application grows;
- interactive navigation should not perform unnecessary duplicate API requests;
- main-thread long tasks above 50 ms should be investigated on common workflows;
- loading indicators should appear immediately for work that cannot complete within a perceptually instant interval;
- large tables should paginate or virtualize rather than render thousands of rows at once.

Bundle thresholds should be made numeric in CI once the first measured release artifact is available.

## Database query review

Before adding an index, identify the real query shape with logs and PostgreSQL query plans.

High-value indexes are expected around:

- unique student admission number;
- active enrollment lookups by student/section/year;
- attendance by student and date;
- marks by student/year/subject;
- announcement publication/date filters;
- audit log timestamp/entity/action filters;
- fields used by supported global-search queries.

Use `EXPLAIN (ANALYZE, BUFFERS)` only on safe non-production or carefully controlled production queries. Avoid running expensive diagnostic queries against a busy production database without operational review.

## Entity Framework guidance

- Use `AsNoTracking()` for read-only queries.
- Project directly to response models instead of loading full graphs when only a subset is needed.
- Keep pagination before materialization.
- Avoid `Include` chains for list endpoints when a targeted projection is sufficient.
- Inspect generated SQL when a LINQ expression becomes complex.
- Do not call database queries inside loops when a set-based query can fetch the required data.
- Keep transaction scopes as short as correctness allows.

## Search

Search inputs and outputs are bounded for both latency and abuse resistance.

- trim and normalize supported search text consistently;
- cap query length;
- cap page size;
- prefer indexed/case-insensitive database operators where supported rather than wrapping indexed columns in transformations that prevent index use;
- measure PostgreSQL `ILIKE`, trigram, or full-text options before adding specialized search infrastructure;
- do not introduce a separate search service until data volume or relevance requirements justify its operational cost.

## Dashboard analytics

Dashboard values should use compact aggregate queries and privacy-conscious cohort rules.

Avoid loading all students, attendance rows, or marks into application memory to calculate summary values. Prefer database aggregation and return only the data required by the visualizations.

When analytics queries become expensive, first review indexes and query shape. If caching is introduced, define:

- cache key including institution/authorization scope;
- maximum staleness;
- invalidation event;
- failure behavior;
- whether stale data may be shown;
- telemetry for hit rate and refresh latency.

## Bulk import/export

Bulk workflows must remain bounded.

- reject files/requests above documented limits;
- stream input/output where practical;
- validate rows before committing irreversible partial state;
- use a transaction for atomic batches where that matches the feature contract;
- report row-level errors without echoing sensitive values unnecessarily;
- measure memory use with the largest supported import size.

For very large future workloads, move long-running jobs to a durable background-job mechanism rather than holding an HTTP request indefinitely.

## Attachments

Attachment upload/download performance must not bypass security checks.

- stream data instead of buffering arbitrarily large files in memory;
- keep explicit size and content-type/extension rules;
- serve downloads using safe storage identifiers rather than user-controlled filesystem paths;
- monitor storage capacity and file-count growth;
- consider object storage only when deployment needs justify it.

## Browser rendering

For data-heavy screens:

- use stable keys;
- avoid recomputing expensive derived data on every render;
- debounce only where it improves network behavior without making controls feel delayed;
- cancel or ignore stale search requests;
- avoid storing duplicated server data in multiple global states;
- lazy-load route code when route size becomes meaningful;
- use CSS responsive layout before introducing JavaScript viewport branching.

## PWA caching

The service worker should prioritize reliable application-shell delivery. Do not broadly cache authenticated API responses unless privacy scope, account switching, retention, invalidation, and offline correctness have been explicitly designed and tested.

A faster stale response is not a performance success when it displays the wrong user's or outdated sensitive data.

## Profiling workflow

When a performance regression is reported:

1. define the exact workflow and data size;
2. capture a baseline on a reproducible environment;
3. separate browser, network, API, database, and storage time;
4. profile the dominant layer;
5. change one bottleneck at a time;
6. re-run the same measurement;
7. add a regression test or benchmark when the path is important and stable enough.

Record measured before/after results in the pull request.

## Observability signals

Production deployments should eventually expose privacy-conscious telemetry for:

- request count, status, and latency by route template;
- database query duration and connection-pool pressure;
- authentication failures and rate-limit rejections without recording credentials;
- import/export duration and row counts;
- attachment operation duration/size buckets;
- Web/PWA route-load and API timing aggregates;
- error rates with correlation identifiers.

Do not include student names, guardian data, tokens, request bodies, raw search text, or attachment content in general performance telemetry.

## Release performance review

Before a major release:

- inspect production Web/PWA bundle output;
- smoke-test primary screens with representative data volume;
- inspect slow API/database operations;
- verify list endpoints remain bounded;
- verify dashboard queries do not scale linearly by performing per-row requests;
- verify container memory stays stable during import/export and attachment operations;
- document newly accepted performance limitations in `what_changed.md` and `ROADMAP.md`.

Performance is considered acceptable only when the product remains usable and predictable under its documented workload while preserving security and correctness.
