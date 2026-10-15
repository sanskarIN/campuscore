# CampusCore — v1.0.0 Release Handoff

Last updated: 2026-10-15 (Asia/Kolkata)
Target: v1.0.0 first stable self-hosted distribution
Repository: https://github.com/sanskarIN/campuscore
Author/committer: Sanskar <sanskarin@outlook.in>

## Release state

The root `VERSION`, .NET package/assembly properties, Web/PWA package/runtime fallback, browser companion package/manifest, Docker Compose, and example environment were moved from 0.2.0 to **1.0.0**. The complete release notes are in `docs/releases/v1.0.0.md`; `CHANGELOG.md`, `ROADMAP.md`, `README.md`, and `docs/release.md` were updated to document actual release scope.

Do not confuse **release preparation** with a published release. The release workflow only publishes GitHub Release v1.0.0 after the final candidate commit succeeds in its own builds and passes all seven same-commit required workflows. A running, pending or failed job is not verification evidence.

## Code / test corrections committed in this preparation

- Fixed React type peer dependency mismatch; TypeScript versus typescript-eslint peer mismatch; nonexistent ESLint package pin.
- Updated MSTest 4 data attributes and exact async exception assertions.
- Made validated bulk import row usage explicit for C# nullability analysis.
- Pinned matching Entity Framework Relational 9.0.19, ASP.NET Identity 9.0.19, JWT 9.0.19, InMemory EF 9.0.19, eliminating missing relational assembly and conflicting version builds.
- Fixed TypeScript Vitest assertion type, Node.js typings, axe Playwright type mismatch.
- Configured service-worker globals for ESLint and removed unused Node import.
- Scoped sign-in E2E click to the form submit button to avoid ambiguity with authentication-mode button.
- Improved light-theme muted text contrast to resolve a recorded accessibility audit issue.

## Release workflow and cross-platform executables

`.github/workflows/release.yml` builds from the checked revision, verifies versions, formatting, .NET tests, Web checks, extension checks, Android assembly and migrations, then publishes on successful gated verification.

- `campuscore-api-v1.0.0-win-x64.zip` — **Windows x64 self-contained API server executable** (`CampusCore.Api.exe`)
- `campuscore-api-v1.0.0-linux-x64.tar.gz` — **Linux x64 self-contained API server executable**
- `campuscore-api-v1.0.0-osx-arm64.tar.gz` — **macOS Apple Silicon self-contained API server executable**
- `campuscore-api-v1.0.0.tar.gz` — framework-dependent API server deployment
- `campuscore-web-v1.0.0.tar.gz` — Web/PWA static distribution
- `campuscore-companion-v1.0.0.zip` — browser companion package
- `campuscore-android-v1.0.0-debug-TEST-ONLY.apk` — Android debug/test verification; non-production example API URL
- `migrations.sql`, `VERSION`, `SHA256SUMS.txt` — migration review, version and integrity

The executable servers **do not** provide standalone desktop GUIs; they require PostgreSQL, secrets, HTTPS and an operator-maintained backend. The Android APK is not store-signed and targets an example test HTTPS endpoint: do not market it as a functional production/Play Store build. Real AAB signing/API URL must be provided by the deployment owner outside Git.

## Action evidence and remaining gate

Validated during preparation from GitHub Actions logs:

- Earlier `@types/react-dom` peer failure, TypeScript/ESLint peer failure, nonexistent ESLint version, obsolete MSTest assertion usage, EF Core 9.0.1/9.0.19 conflict, API startup assembly errors, Web TS errors, service-worker lint globals, E2E login selector collision, and CSS contrast issue all had identified corrective commits.
- On the intermediate `e3d1554b` revision: Performance Budget, CodeQL and Android workflows completed **successfully**. Other workflows on that intermediate commit failed or were cancelled due to additional defects/ongoing commits. Intermediate green results do **not** satisfy the final release requirement.
- After fixes and the version synchronization, final end-to-end quality, migration, recovery, deployment and artifact validation must be verified on the **same final commit** before automatic tag/release publication.

Required passing statuses for final v1.0.0 SHA: **CI, CodeQL, Deployment Smoke, Full-stack E2E, Performance Budget, Android, Version Consistency**. The release workflow itself must also pass.

## Release execution

1. Confirm versions with `node scripts/verify-version.mjs --tag v1.0.0`.
2. Publish a finalization commit with message starting `chore(release): finalize v1.0.0`, touching `.github/workflows/release.yml` so all seven required workflows are dispatched.
3. The release workflow compiles all packages, waits for seven successful workflow conclusions on that same SHA, creates an annotated tag using `sanskarin@outlook.in`, then creates the GitHub Release with downloadable assets.
4. If any check fails, no release should be asserted; inspect its job logs, commit a fix, rerun the finalization gate from a new commit, and review results.
5. Confirm release existence, the expected asset files, and SHA-256 values through GitHub after publication.

## Follow-ups not incorrectly counted as v1.0.0 production features

- Owner-provisioned production Android AAB signing, HTTPS backend deployment, physical-device acceptance testing.
- Browser companion marketplace packaging/listing approval.
- Reviewed npm lockfile and npm ci migration.
- Stronger release provenance/attestation and migration upgrade-path testing.

MIT licensed. Made by the Sanskar.
