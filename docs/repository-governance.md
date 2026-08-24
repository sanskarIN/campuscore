# Repository governance and release controls

This document covers repository settings that cannot be enforced by committed application code alone. Source-controlled checks complement GitHub branch/ruleset configuration; they do not replace it.

## Current release-control expectation

Before tagging a production-intended CampusCore release, the default branch should be protected by a GitHub ruleset or branch-protection rule. The repository currently contains `CODEOWNERS`, CI workflows, version checks, release-asset checks, and release documentation so those settings can require concrete checks rather than relying on convention.

## Recommended `main` rules

Configure the repository so `main` requires:

- pull requests for normal changes;
- at least one approving review when more than one trusted maintainer is available;
- CODEOWNERS review for security/release-sensitive paths when practical;
- dismissal or re-approval when reviewed commits change;
- successful required status checks before merge;
- branches to be up to date with `main` when the project uses merge-based integration;
- conversation resolution before merge;
- no force pushes;
- no branch deletion;
- administrator bypass limited to genuine recovery situations and audited when used.

For a single-maintainer repository, requiring the maintainer to approve their own pull request may not be useful or possible. Keep the non-review controls—required checks, no force pushes, no deletion, and controlled bypass—even when review count is zero.

## Candidate required checks

Use the actual check names produced by GitHub Actions rather than copying names blindly from this document. The intended release gate includes jobs/workflows covering:

- backend formatting/build/tests and NuGet vulnerability review;
- Web type-check/lint/unit/build/audit;
- deterministic Playwright E2E and accessibility;
- real full-stack browser release smoke;
- operations script parsing;
- Docker Compose validation;
- database migration integrity;
- idempotent migration SQL generation;
- backup/restore recovery drill;
- production deployment smoke;
- Web release bundle safety/performance budget;
- Android project regeneration and debug APK assembly;
- browser companion validation/package checks;
- repository version consistency;
- CodeQL.

Do not mark an intermittently failing check as optional just to unblock a release. Fix the flaky or incorrect check first.

## Tag and release controls

The release workflow accepts tags matching `v*`, but the workflow immediately verifies that the tag equals `v` plus the root `VERSION` value. For the prepared candidate that means `v0.2.0`.

Recommended repository settings/process:

1. restrict who can create release tags when the hosting plan/ruleset supports tag rules;
2. create the tag only from a commit that passed the required branch checks;
3. prefer a signed tag under the maintainer's approved signing policy;
4. do not move or reuse an existing release tag;
5. do not manually replace generated release assets after publication without documenting the correction and regenerating checksums;
6. treat `VERSION`, `migrations.sql`, and `SHA256SUMS.txt` as part of the release evidence.

## Workflow security

For workflows that only read the repository, keep `permissions: contents: read`. Grant broader permissions only to the job/workflow that needs them. The release workflow requires `contents: write` to create a GitHub Release; routine test workflows do not.

Additional hardening to consider for a later release:

- pin third-party GitHub Actions to reviewed immutable commit SHAs rather than moving major-version tags;
- enable dependency review for pull requests when available;
- enable secret scanning/push protection and private-vulnerability reporting where supported;
- add artifact attestations/provenance for release archives and future container images;
- use protected environments for any future production deployment/signing workflow.

## CODEOWNERS

`.github/CODEOWNERS` assigns the repository owner to all files and explicitly lists security/release-sensitive paths. CODEOWNERS only becomes an enforcement mechanism when the corresponding GitHub review/ruleset setting is enabled.

## Release-candidate checklist

Before `v0.2.0`:

- [ ] `main` protection/ruleset is enabled with required checks appropriate to the repository's maintainer model.
- [ ] force pushes and deletion of `main` are disabled.
- [ ] final candidate commit has green required checks.
- [ ] no required workflow is skipped because its path filter failed to include a release-sensitive file.
- [ ] version consistency passes for `0.2.0`.
- [ ] no unresolved blocker/critical security issue remains.
- [ ] release notes and `what_changed.md` reflect the exact tag commit.

Repository settings should be reviewed again whenever workflows are renamed, split, or consolidated because required-check names can change.
