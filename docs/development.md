# CampusCore Development Guide

## Working agreement

CampusCore favors small, reviewable changes over broad rewrites. A feature is not complete when only the happy-path endpoint or screen exists; authorization, validation, loading/error states, migration impact, audit behavior, accessibility, tests, and documentation are part of the same change.

Use focused Conventional Commit messages where practical:

```text
feat(students): add guardian workflow
fix(auth): reject expired browser sessions
test(domain): cover academic year validation
docs: explain release rollback
```

## Repository layout

```text
CampusCore.sln
src/
  CampusCore.Domain/          entities, enums, invariants
  CampusCore.Application/     use cases, DTOs, service abstractions
  CampusCore.Infrastructure/  EF Core, Identity, storage, audit implementations
  CampusCore.Api/             HTTP host, auth, middleware, endpoints
  CampusCore.Web/             React/TypeScript Web/PWA + Capacitor source
  CampusCore.Extension/       minimal Manifest V3 browser companion
tests/
  CampusCore.Domain.Tests/    domain unit tests
  CampusCore.Application.Tests/
  CampusCore.Infrastructure.Tests/
  CampusCore.Api.Tests/
docs/                         maintainer and operator documentation
.github/                      CI, Android/extension checks, CodeQL, release, Dependabot and templates
```

The generated `src/CampusCore.Web/android/` tree is intentionally ignored. Reproducible native behavior belongs in web source, Capacitor configuration, plugins, scripts, or documented Gradle customization that can be recreated safely.

## Backend workflow

### Domain

Keep domain classes infrastructure-free. Put simple invariants close to the entity when they are meaningful independent of transport/database concerns.

Examples:

- admission number and required student name;
- academic-year date ordering;
- computed mark percentage.

Do not reference EF Core, ASP.NET Core, HTTP types, or configuration in the domain project.

### Application

Application services coordinate use cases and depend on abstractions such as the database context, audit writer, or file storage interface. They should:

- validate cross-entity state;
- enforce transactional boundaries;
- return purpose-specific models rather than exposing EF entities accidentally;
- keep business behavior reusable from HTTP or future entry points.

### Infrastructure

Infrastructure owns technology implementations:

- PostgreSQL/EF Core mappings and migrations;
- ASP.NET Core Identity persistence;
- local attachment storage;
- audit persistence;
- database initialization/seeding.

Never put deployment secrets in infrastructure source files.

### API

Minimal API endpoint modules translate HTTP concerns to application calls. Each mutating endpoint should review:

1. authentication requirement;
2. role policy;
3. payload validation;
4. safe not-found/conflict/error semantics;
5. audit implications;
6. OpenAPI discoverability.

Do not rely on the React client's role visibility for authorization.

## Database changes

When persistent schema changes:

1. update the domain/application/infrastructure model;
2. generate an EF migration;
3. inspect the generated migration and model snapshot;
4. test applying it to a disposable database;
5. test upgrading from the previous supported schema;
6. document any downtime or data transformation in `what_changed.md` and release notes.

Typical command:

```bash
dotnet ef migrations add <MeaningfulName> \
  --project src/CampusCore.Infrastructure \
  --startup-project src/CampusCore.Api
```

Never edit an already released migration merely to make history look cleaner. Add a new corrective migration.

## Web/PWA workflow

### Project structure

```text
src/CampusCore.Web/
  public/                manifest, service worker, editable logo
  scripts/               build/budget/native validation scripts
  src/
    api/                 authenticated fetch abstraction
    auth/                browser session context/storage
    components/          reusable product components
    hooks/               focused React hooks
    pages/               route-level workflows
    platform/            web/native runtime boundary
    theme/               appearance preference
    App.tsx              routes and role guards
    main.tsx             browser/native bootstrap
    styles.css           shared design tokens and responsive styles
    native.css           native safe-area/touch adjustments
  capacitor.config.ts    reproducible native application identity/config
```

### API calls

Use `apiRequest`, `apiJson`, or `apiDownload` instead of calling `fetch` directly from feature pages unless a new protocol requirement justifies it. This preserves:

- bearer token behavior;
- cookie omission;
- consistent problem parsing;
- `401` session invalidation;
- environment URL handling.

### Sensitive browser state

Do not put access tokens, student records, audit details, or API responses in `localStorage`.

Current policy:

- JWT session: `sessionStorage`;
- theme preference: `localStorage`;
- authenticated API data: React memory only;
- service worker: public app-shell resources only.

Any change to this boundary requires a privacy/security review and ADR update.

### Routing and authorization

React route guards are usability controls. They prevent an administrator page from being linked/rendered to an ordinary role, but they do not provide security. API authorization must independently reject unauthorized requests.

### Forms

Forms should include:

- an explicit label for each control;
- native input types where useful;
- required/min/max constraints as early feedback;
- server error rendering with `role="alert"`;
- disabled/busy behavior during submission;
- a success state after mutation;
- no secret value persistence.

### Responsive UI

Test at minimum:

- narrow mobile width around 320–390 CSS px;
- tablet width around 768 CSS px;
- desktop width 1280+ CSS px;
- keyboard-only navigation;
- light and dark appearance;
- browser zoom at 200%.

Avoid fixed widths that make tables or forms unusable. Wide datasets should use a horizontally scrollable table wrapper rather than shrinking text below readable sizes.

## PWA changes

`public/sw.js` is intentionally small. Before changing cache behavior ask:

- Can this response contain student, guardian, staff, auth, audit, or institution-private data?
- Will the browser retain the response after sign-out?
- Can another account on the same device see stale data?

Do not cache `/api/*` under the existing architecture.

When changing precached shell files, increment `CACHE_NAME` so obsolete caches are removed on activation.

## Android changes

Android reuses the web client through Capacitor. Read `docs/android.md` before changing native behavior.

Rules:

- Keep `capacitor.config.ts` and direct Capacitor versions committed and pinned.
- Keep generated `android/` output out of Git.
- Keep production API targets HTTPS-only.
- Preserve `https://localhost` in the deployed API CORS configuration for the native webview.
- Do not register the PWA service worker inside a native runtime.
- Put cross-platform behavior behind `src/platform/` rather than scattering Capacitor checks through feature pages.
- Never commit a signing keystore or signing password.

For a local native verification with a safe test origin:

```bash
cd src/CampusCore.Web
VITE_API_BASE_URL=https://api.example.test npm run build:android
npx cap add android
npx cap sync android
```

Use `npm run android:sync` once a generated Android project already exists.

## Browser companion changes

The current extension is intentionally navigation-only and least-privilege. `src/CampusCore.Extension/validate.mjs` enforces its security boundary.

Before adding a permission, content script, host permission, or API call:

1. document the exact user-facing feature that requires it;
2. confirm the feature cannot be implemented through normal CampusCore navigation;
3. update the threat/privacy documentation;
4. add validation/tests that constrain the new permission to the minimum required scope;
5. review Chrome/Edge store policy implications.

Validate the extension with:

```bash
cd src/CampusCore.Extension
npm run check
```

## Formatting and checks

Backend:

```bash
dotnet restore CampusCore.sln
dotnet format CampusCore.sln --verify-no-changes --no-restore
dotnet build CampusCore.sln -c Release --no-restore -warnaserror
dotnet test CampusCore.sln -c Release --no-build
```

Web/PWA:

```bash
cd src/CampusCore.Web
npm install
npm run typecheck
npm run lint
npm run test
npm run build
```

Android web/native source:

```bash
cd src/CampusCore.Web
VITE_API_BASE_URL=https://api.example.test npm run build:android
```

Browser companion:

```bash
cd src/CampusCore.Extension
npm run check
```

Full container configuration:

```bash
docker compose config --quiet
```

## Dependency changes

- Use maintained stable releases compatible with the project's runtime targets.
- Pin direct web/native dependencies in `package.json`.
- Review transitive vulnerability output before merging.
- Keep GitHub Actions on supported major versions and allow Dependabot to propose updates.
- Do not update dependencies solely for version-number freshness when the new version breaks runtime support.
- Keep Android and Capacitor package majors aligned unless the upstream compatibility matrix explicitly supports another combination.

The web package currently has no committed npm lockfile, so clean automation uses `npm install`. A future lockfile must be generated by a networked clean checkout, reviewed, committed with the dependency graph it represents, and then CI/release commands should move to `npm ci`.

## Logging and diagnostics

Never log:

- passwords;
- bootstrap keys;
- JWTs;
- database credentials;
- complete student/guardian records;
- uploaded document contents.

Use correlation IDs, entity IDs, action names, and safe metadata instead.

## Definition of done

A change is ready when:

- clean restore/build succeeds;
- relevant tests pass;
- lint/type checks pass for web changes;
- Android build validation passes for native-impacting web changes;
- extension validation/package checks pass for browser-companion changes;
- no high-severity dependency audit failures remain unresolved;
- role/authorization behavior was reviewed;
- database migrations are present when required;
- keyboard/focus/responsive states were checked for UI work;
- deployment/configuration changes are documented;
- `what_changed.md` and CHANGELOG/README are updated when the change affects users or maintainers.
