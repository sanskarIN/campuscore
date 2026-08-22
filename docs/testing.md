# Testing CampusCore

CampusCore uses layered verification so failures are caught as close to their source as possible. The repository should never rely on a single end-to-end suite as its only safety net.

## Quality gates

Run these checks before opening or merging a pull request:

```bash
dotnet restore CampusCore.sln
dotnet format CampusCore.sln --verify-no-changes
dotnet build CampusCore.sln --configuration Release --no-restore
dotnet test CampusCore.sln --configuration Release --no-build

cd src/CampusCore.Web
npm ci
npm run typecheck
npm run lint
npm run test
npm run build
```

When Docker is available, also validate the deployment definition:

```bash
docker compose config --quiet
```

The GitHub Actions CI workflow is the authoritative clean-environment check because it restores dependencies from scratch.

## Test pyramid

### Domain unit tests

Location: `tests/CampusCore.Domain.Tests`

Domain tests cover business invariants that do not need a database or HTTP host. They should be deterministic, fast, and independent of wall-clock timing wherever possible.

Required coverage includes:

- student identity and admission-data validation;
- academic-year date boundaries;
- attendance and mark value constraints;
- grade-scale range validation;
- enrollment and timetable invariants as those rules evolve.

Every production bug caused by a domain invariant should receive a regression test when the behavior can be reproduced at this layer.

### Application tests

Application-service tests should verify use cases such as student creation, guardian changes, enrollment, attendance updates, mark recording, report-card calculation, search pagination, imports, and audit emission.

Prefer an isolated relational test database over mocking Entity Framework query behavior. SQLite can be useful for pure relational behavior, but PostgreSQL-backed integration tests are required for PostgreSQL-specific constraints or SQL semantics.

### API integration tests

API tests should exercise the real ASP.NET Core routing, authentication, authorization, validation, safe problem responses, rate-limit behavior where practical, and security headers.

Integration tests must not depend on production credentials. Use a disposable PostgreSQL database or a CI service container seeded only with fictional data.

Critical API scenarios:

- anonymous requests cannot reach protected student or administration routes;
- invalid sign-in attempts never disclose whether an account exists;
- role-protected routes reject insufficient privileges;
- duplicate admission numbers return a safe conflict response;
- invalid attachment metadata or oversized uploads are rejected;
- search and bulk operations enforce configured limits;
- health checks report readiness without exposing secrets.

### Web unit and component tests

Location: `src/CampusCore.Web`

Vitest is the browser-client test runner. New UI behavior should be factored so parsing, formatting, permission decisions, reducers, query construction, and other deterministic logic can be tested without network access.

Component tests should focus on behavior rather than implementation details:

- accessible names and labels;
- keyboard interaction;
- loading, empty, error, and success states;
- role-based action visibility;
- theme and reduced-motion preferences;
- expired-session recovery;
- offline messaging.

### End-to-end tests

End-to-end coverage should use a disposable full stack and focus on a small set of high-value journeys rather than duplicating lower-level coverage.

Minimum release-candidate journeys:

1. administrator signs in and reaches the dashboard;
2. administrator creates a student and primary guardian;
3. administrator enrolls the student into a section;
4. authorized staff records attendance and marks;
5. authorized user views a report card;
6. administrator publishes an announcement;
7. administrator changes institution settings and verifies audit history;
8. user signs out and protected routes become inaccessible.

## Database and migration verification

A release candidate must prove both clean installation and upgrade safety.

For a clean database:

```bash
dotnet ef database update \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api
```

For migration review, generate an idempotent SQL script and inspect it before deployment:

```bash
dotnet ef migrations script --idempotent \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api \
  --output artifacts/migrations.sql
```

Never edit a previously released migration to change production history. Add a new migration instead.

## Security checks

CI should run maintained dependency and static-analysis checks. Locally, also review:

```bash
dotnet list CampusCore.sln package --vulnerable --include-transitive
npm --prefix src/CampusCore.Web audit --omit=dev
```

Treat audit output as evidence that needs review, not as an automatic guarantee. Confirm exploitability and affected runtime paths before accepting or suppressing findings.

## Test data rules

- Use fictional people, institutions, email addresses, and identifiers only.
- Do not copy production databases into local or CI test environments.
- Keep fixtures deterministic and minimal.
- Do not commit JWT signing keys, passwords, API tokens, or real attachment data.
- Randomized/property tests must print a reproducible seed on failure.

## Flaky tests

A flaky test is a defect. Do not repeatedly re-run a failing test until it passes and then merge.

When a test flakes:

1. record the failure and environment;
2. determine whether time, concurrency, external I/O, ordering, or shared mutable state is involved;
3. make the test deterministic or fix the underlying race;
4. add regression coverage for the discovered condition;
5. remove any temporary quarantine before release.

## Coverage philosophy

CampusCore does not optimize for a single percentage. Coverage must be risk-based. Authentication, authorization, academic calculations, student mutations, imports, uploads, audit behavior, migrations, and permission boundaries deserve deeper coverage than passive presentation code.

A release candidate is not ready when important behavior remains untested merely because an aggregate line-coverage number is high.
