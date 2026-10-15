# CampusCore — v1.0.0 Release Handoff

## Final v1.0.0 website-linked release — VERIFIED (2026-10-15)

- **Published stable release:** https://github.com/sanskarIN/campuscore/releases/tag/v1.0.0.
- **New release ID:** `413624847`, published `2026-10-15T09:24:08Z`; the previously rebuilt release was replaced only after successful gate and package verification.
- **Final annotated tag:** `v1.0.0`, annotated tag SHA `e734d8e40d648a8a789b5ad11bf0c892a4e3512b`, target commit `0429bdbc9b7b678ffa7b11de7aaee247436be764`, tagger Sanskar <sanskarin@outlook.in>.
- **Release workflow:** https://github.com/sanskarIN/campuscore/actions/runs/38712020960 — completed **success**.
- **All eight workflows on final tag commit were successful:** CI, CodeQL, Deployment Smoke, Full-stack E2E, Android, Performance Budget, Version Consistency, Release.
- **Ten new nonempty downloadable files with SHA-256 digests:** self-contained Windows/Linux/macOS API servers, framework-dependent server archive, Web/PWA, Chromium companion, debug/test-only Android APK, migration SQL, checksum manifest and VERSION. Release job verified SHA-256 checksums and archive integrity before publication.
- **Official website:** https://sanskarin.github.io, visible from the About page and mentioned in README, SUPPORT.md and published GitHub release description. This is a portfolio website **not** an API or hosted CampusCore server.
- **Current optional donations:** https://buymeacoffee.com/SanskarCodes. The retired support mailbox has been removed from public Web/PWA UI and release docs. The built Web/PWA assets passed explicit website/BMC presence and retired-contact exclusion checks.
- The Web/PWA About-page browser test passed in CI, including correct website/BMC hrefs and two active business email contacts.
- **Do not reuse the tag:** After publication the release workflow was hardened to reject replacing existing versions. Any subsequent binary release must have a new VERSION, tag, and checksum manifest.
- Earlier v1.0.0 downloads are superseded; redownload and verify the checksum manifest of the currently published artifacts.
- **Limits:** API packages are server executables requiring PostgreSQL/configuration, Android debug APK uses an example API origin and is not a Play Store-ready signed application.

## Final v1.0.0 author-website release — final quality gate

- Requested website: https://sanskarin.github.io (portfolio and author website, **not** the CampusCore API backend).
- Updated project README, support guide, v1.0.0 release description, and in-app About page; the same React UI is included in the Capacitor Android debug/test APK.
- The official optional donation URL remains https://buymeacoffee.com/SanskarCodes; the retired support mailbox remains absent from all public UI and documentation.
- Added a Playwright regression that checks About-page website/BMC links and the two active business contacts.
- Release workflow audits tracked files and built Web/PWA assets for public links and the retired mailbox, assembles .NET API servers, Web/PWA, browser companion and Android test APK, generates SQL and checksums, then gates publication on seven same-commit workflow successes.
- For this **explicitly requested final v1.0.0 replacement**, the old tag, release and downloads stay public until all packages and checks pass. Only then can GitHub Actions replace the old release and reattach newly verified assets.
- Status at documentation time: **Final rebuild not yet confirmed published**. Check the resulting Release workflow run and GitHub Release API before claiming that this specific final build is complete.

## Latest status: Rebuilt v1.0.0 replacement VERIFIED — October 15, 2026

- **New official release:** https://github.com/sanskarIN/campuscore/releases/tag/v1.0.0
- **Release publication:** 2026-10-15T09:11:39Z, new release ID `413613709` (the original release ID `413565340` was deleted).
- **New annotated tag:** `v1.0.0` resolves to verified Git commit `aa506acd974cfd740d7d2be3d5be963d5dc9279e`; tag author `Sanskar <sanskarin@outlook.in>` (tagged 2026-10-15T09:11:31Z).
- **Eight successful workflows on the tag commit:** CI, CodeQL, Deployment Smoke, Full-stack E2E, Performance Budget, Android, Version Consistency, Release.
- **Successful replacement release workflow:** https://github.com/sanskarIN/campuscore/actions/runs/38710674745.
- **Artifacts:** 10 newly generated nonempty downloadable files, each with a GitHub SHA-256 digest: Windows x64, Linux x64 and Apple Silicon macOS API servers; framework-dependent API, Web/PWA, companion, test-only Android APK, `migrations.sql`, `SHA256SUMS.txt` and `VERSION`.
- **Source/product audit:** Release workflow's `Audit retired support and donation references` passed. Built Web/PWA validated it contains `https://buymeacoffee.com/SanskarCodes` and excludes retired contact and funding URLs.
- **Action for previous downloaders:** The user-requested replacement of the already-public v1.0.0 tag and assets invalidates the original v1.0.0 archive hashes. Redownload and recheck the new `SHA256SUMS.txt`.
- **Limitations unchanged:** Executables are server programs requiring configured PostgreSQL; Android APK is debug/test-only with an example HTTPS API URL, not a Google Play production build.

## Post-publication Buy Me a Coffee correction — 2026-10-15

- **Current official Buy Me a Coffee link:** https://buymeacoffee.com/SanskarCodes (replaces the earlier sanskarIN path).
- Updated `.github/FUNDING.yml`, the README badge and contact link, `SUPPORT.md`, and the Web/PWA About page source `src/CampusCore.Web/src/pages/AboutPage.tsx`.
- Updated `docs/releases/v1.0.0.md` and synced those notes into the **existing, published** v1.0.0 GitHub Release: https://github.com/sanskarIN/campuscore/releases/tag/v1.0.0.
- Verified the GitHub Actions **Published Release Notes Sync** completed successfully: https://github.com/sanskarIN/campuscore/actions/runs/38708193916.
- The author-requested replacement has completed: all seven same-commit checks and rebuilt archive integrity validation passed before replacing the tag, release and ten downloadable assets.
- Removed the retired support mailbox from the current README, SUPPORT.md, SECURITY.md, CODE_OF_CONDUCT.md, About page, and v1.0.0 release notes. Confidential reports to the security/conduct team use the business email until a new private contact channel is provided.
- **Replacement verification status: CONFIRMED** via the published release API, new annotated tag target, new file digests and completed release workflow.
- Because the same version tag will point to a newly built revision, anyone who downloaded the original v1.0.0 assets must redownload and validate the new SHA-256 checksums.
- Changes to this project were pushed on main in focused commits, authored and committed by Sanskar <sanskarin@outlook.in>.

## Original v1.0.0 publication — verified historical record, superseded by replacement

**Status: PUBLISHED — 2026-10-15.**

- Public GitHub Release: https://github.com/sanskarIN/campuscore/releases/tag/v1.0.0
- Release title: `CampusCore v1.0.0`; not a draft or prerelease.
- Annotated tag `v1.0.0` points to exact commit `3bf2545e0e035678fdc17006000649b004c7a979`.
- Tagger: `Sanskar <sanskarin@outlook.in>`; created 2026-10-15T08:16:04Z.
- Release published 2026-10-15T08:16:10Z.
- All checks **SUCCESS** for release commit: CI, CodeQL, Deployment Smoke, Full-stack E2E, Performance Budget, Android, Version Consistency, and Release.
- GitHub verified **10 attached nonempty assets** with SHA-256 digests: Windows x64 API server ZIP, Linux x64 API server archive, Apple Silicon API server archive, framework-dependent API archive, Web/PWA archive, browser companion ZIP, Android **debug/test-only** APK, idempotent SQL migration script, VERSION, and SHA256SUMS.txt.
- GitHub Actions release packaging also validated archive integrity and all SHA-256 checksums before publication.
- Product-specific deployment limitations remain as documented: self-hosted backend/database, non-store-signed Android test APK with placeholder API URL, and a browser companion not yet published to a store.
- Post-release status updated in README.md and ROADMAP.md, each with focused commits.

The sections below retain engineering and verification history from the v1.0.0 release preparation. Their earlier "pending release" notes reflect *intermediate* revisions and are superseded by the verified publication above.


## Final v1.0.0 release blocker addressed (2026-10-15)

The last reviewed candidate `f5451efa` passed six mandatory gates (CI, CodeQL, Deployment Smoke, Performance Budget, Android, Version Consistency). Full-stack E2E alone failed due to a Playwright strict selector collision while an announcements loading indicator and a success notice simultaneously carried the `status` role. The real API had successfully published the announcement. Fixed the test to target exact success copy for both announcement publication and institution-settings updates. Disabled automatic retries for the one stateful real-stack acceptance test, whose first administrator setup cannot be repeated against the same persistent CI test database without cleanup. Updated its synthetic fixture labels to v1.0.0.

The finalization workflow must still prove that all seven mandatory checks and all release-build steps pass on the new final source commit before the tag and public GitHub Release can be considered successful.


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
- Added EF Core design-time package reference to the API startup project so `dotnet ef migrations script` can generate the release SQL.
- Explicitly pinned migration ledger table to `public.__EFMigrationsHistory` for consistent migrations on restart/restore. CI now stops the API before recreating it to avoid overlapping migration startup.
- Fixed TypeScript Vitest assertion type, Node.js typings, axe Playwright type mismatch.
- Configured service-worker globals for ESLint and removed unused Node import.
- Scoped sign-in E2E click to the form submit button and differentiated the authentication-mode button's accessible label.
- Fixed API class/section endpoint serialization to return finite DTO projections rather than cyclic EF navigation graphs.
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
