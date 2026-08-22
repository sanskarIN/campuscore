# CampusCore Release Guide

This document defines the release process for CampusCore. A release is a reproducible engineering event, not just a Git tag.

## Versioning

CampusCore follows Semantic Versioning once public stable releases begin:

- `MAJOR`: incompatible API, configuration, or migration expectations;
- `MINOR`: backward-compatible features;
- `PATCH`: backward-compatible fixes and security hardening.

Pre-release tags may use identifiers such as `v0.2.0-rc.1`.

## Release prerequisites

Before creating a release candidate:

- `main` contains only reviewed, intended changes;
- `CHANGELOG.md`, `ROADMAP.md`, and `what_changed.md` describe the real repository state;
- no real credentials, user data, or private endpoints are present;
- the API, Web/PWA, tests, Docker definitions, and documentation all match the intended version;
- every database change is represented by an EF Core migration;
- known blocker or critical defects are resolved;
- dependency and static-analysis findings have been reviewed.

## Clean verification

Run from a clean checkout with no generated build directories.

```bash
dotnet restore CampusCore.sln
dotnet format CampusCore.sln --verify-no-changes
dotnet build CampusCore.sln --configuration Release --no-restore
dotnet test CampusCore.sln --configuration Release --no-build

npm --prefix src/CampusCore.Web ci
npm --prefix src/CampusCore.Web run check

docker compose config --quiet
docker compose build --pull
```

Bring up the disposable stack and verify health:

```bash
docker compose up -d
curl --fail http://localhost:8080/healthz
curl --fail http://localhost:8081/
docker compose down -v
```

Use platform-appropriate HTTP tooling when `curl` is unavailable.

## Database migration review

Generate an idempotent migration script:

```bash
mkdir -p artifacts
dotnet ef migrations script --idempotent \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api \
  --output artifacts/migrations.sql
```

Review the script for destructive operations, long locks, table rewrites, index creation cost, unexpected default values, and data-loss risk. Production backups and rollback procedures must exist before applying a destructive or irreversible migration.

## Security review

At minimum:

```bash
dotnet list CampusCore.sln package --vulnerable --include-transitive
npm --prefix src/CampusCore.Web audit --omit=dev
```

Also confirm GitHub CodeQL and repository secret/dependency checks are successful when available.

Review release configuration for:

- a production-only JWT signing key supplied by a secret store;
- explicit allowed CORS origins;
- HTTPS termination and secure proxy headers;
- least-privilege PostgreSQL credentials;
- attachment-storage persistence, quotas, and backup policy;
- log destinations and retention;
- production error responses that contain no stack traces or secrets.

## Release artifacts

The release workflow should produce reproducible artifacts from the tagged source. Expected artifacts include:

- published ASP.NET Core API output;
- built Web/PWA static assets;
- checksums for downloadable archives;
- generated release notes or a changelog excerpt.

Container registries may be added later, but images must use immutable version tags in addition to any moving convenience tag.

## Tagging

Only tag a commit after the release-candidate checks pass.

```bash
git tag -s v0.2.0 -m "CampusCore v0.2.0"
git push origin v0.2.0
```

If signed tags are not available in the release environment, use the repository's approved signing policy and record the exception.

## Smoke-test checklist

After deployment, verify:

- API readiness endpoint responds successfully;
- Web/PWA shell loads over HTTPS;
- sign-in works with an authorized test account;
- unauthorized users cannot access protected routes;
- dashboard data loads;
- student search and student detail views work;
- one reversible student or academic mutation succeeds and is audited;
- announcement and attachment paths behave as expected;
- report-card rendering works;
- no browser console errors or unexpected server errors appear;
- security headers are present at the public edge.

Use fictional release-test records and remove them afterward if the environment is persistent.

## Rollback strategy

Application rollback and database rollback are different concerns.

For application failures:

1. stop further rollout;
2. restore the last known-good immutable application/container artifact;
3. verify health and primary workflows;
4. preserve logs and correlation identifiers for investigation.

For database failures, prefer forward-fix migrations. Only use a database restore or explicit rollback when the migration and operational runbook have been reviewed for data safety.

Never assume application rollback automatically makes a migrated database compatible with an older binary.

## Release notes

Release notes should include:

- user-visible features and fixes;
- security-relevant changes without exploit-enabling detail;
- migration/configuration changes;
- upgrade and rollback notes;
- known limitations;
- contributor acknowledgements when appropriate;
- support and responsible-disclosure links.

## Post-release

After a successful release:

- update `CHANGELOG.md` from `Unreleased` to the released version/date;
- move completed roadmap items;
- update `what_changed.md` with the tag, commit, verification evidence, and next milestone;
- verify GitHub release artifacts are downloadable and checksums match;
- monitor error rate, latency, database health, storage growth, and authentication failures;
- create issues for non-blocking follow-up work discovered during release.
