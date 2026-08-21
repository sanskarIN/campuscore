# CampusCore Architecture

## Overview

CampusCore is a modular monolith with a separately built React Web/PWA client. The design optimizes for a small maintainer team, explicit data ownership, auditable business operations, and straightforward deployment.

```text
Browser / installed PWA
        |
        | HTTPS / JSON
        v
CampusCore.Web (React + TypeScript)
        |
        v
CampusCore.Api (ASP.NET Core minimal APIs)
        |
        v
CampusCore.Application
        |
        +--------------------+
        |                    |
        v                    v
CampusCore.Domain      abstractions/ports
                             |
                             v
                    CampusCore.Infrastructure
                             |
                  +----------+-----------+
                  |                      |
                  v                      v
              PostgreSQL           local file store
```

## Dependency rule

Dependencies point inward:

1. `CampusCore.Domain` owns entities, enums, and domain-level invariants. It has no infrastructure dependency.
2. `CampusCore.Application` owns use-case services, request/response models, and infrastructure abstractions. It references the domain.
3. `CampusCore.Infrastructure` implements persistence, Identity storage, auditing, file storage, and database initialization. It references application/domain contracts.
4. `CampusCore.Api` composes the process, authentication/authorization, middleware, OpenAPI, rate limiting, and HTTP endpoints.
5. `CampusCore.Web` is an independent HTTP client. It never imports backend assemblies or bypasses HTTP authorization.

## Backend modules

### Identity and access

ASP.NET Core Identity stores users and roles. JWT access tokens are issued by the API after login or the one-time first-administrator bootstrap operation. Endpoint authorization remains the source of truth; frontend role checks only improve navigation and cannot grant access.

Roles currently exposed by product workflows are:

- `Administrator` — institution configuration, users/roles, catalog, staff, reporting, and all operational tasks.
- `Registrar` — student lifecycle, enrollment, attendance, timetable, announcements, exports, and related operational tasks.
- `Teacher` — classroom-facing attendance/assessment operations allowed by API policy.

Any new sensitive endpoint must state its authorization requirement explicitly.

### Students and guardians

Student identity, contact information, active state, guardians, and enrollment history are modeled separately so relationship data can evolve without duplicating a student profile. Admission numbers are unique database identifiers in addition to the internal GUID primary key.

### Academic catalog

Academic years, school classes, sections, subjects, and grade scales are reference data. Operational records reference catalog GUIDs rather than copying mutable display values.

### Academic operations

Attendance, marks, grades, leave, enrollment, timetable entries, and report-card aggregation are application services/endpoints built on domain entities. Mutating workflows validate both payload shape and cross-record constraints before persistence.

### Communication and attachments

Announcements are audience-targeted records with optional attachments. Attachment bytes are stored outside the relational database by `IFileStorage`; metadata and ownership remain in PostgreSQL. Generated storage names, extension normalization, allow-list validation, and size limits reduce path traversal and unsafe upload risks.

### Administration and audit

Institution settings and account/role administration are administrator-only. Sensitive mutations write privacy-conscious audit records containing identifiers and safe metadata instead of full PII snapshots or secrets.

## Persistence

Entity Framework Core targets PostgreSQL. Migrations live with infrastructure persistence and are the only supported schema evolution mechanism.

Key practices:

- unique indexes back externally meaningful identifiers;
- foreign keys preserve relationship integrity;
- application services use transactions where multi-record changes must be atomic;
- database constraint conflicts are translated to safe HTTP problem responses instead of exposing provider details;
- migration scripts must be reviewed before production use.

## Web/PWA architecture

`src/CampusCore.Web` uses React, TypeScript, Vite, React Router, and native browser APIs.

### State boundaries

- Authentication: `AuthContext`, with access token stored in `sessionStorage` only.
- Appearance: `ThemeContext`, with non-sensitive theme preference stored in `localStorage`.
- Server data: focused page-level resource hooks and centralized `apiRequest` helpers.
- Navigation/filter state: URL search parameters where shareable/reload-safe state is useful.

The app intentionally avoids a large global state container until a demonstrated cross-screen requirement warrants one.

### API boundary

`src/api/client.ts` is the single authenticated fetch abstraction. It:

- prepends the configured API base URL;
- sends the bearer token when present;
- omits cookies;
- serializes JSON consistently;
- translates problem responses into `ApiError`;
- invalidates the browser session on authenticated `401` responses;
- keeps blob downloads on the same authorization path.

### PWA boundary

The service worker caches only public application-shell resources. Requests under `/api/` are deliberately excluded. Private student/staff/administration responses therefore do not become offline cache entries.

The installed PWA can reopen its shell while offline, but authenticated operations require the API connection.

## Deployment topology

The included Compose topology is:

```text
:8081 -> nginx/Web PWA -> /api/* -> API:8080 -> PostgreSQL:5432
                                     |
                                     +-> /data/uploads persistent volume
```

This gives the browser a same-origin API in the container deployment, allowing a restrictive web Content Security Policy and avoiding deployment-specific CORS exposure.

For local hot reload, run PostgreSQL in Compose and start API/Web directly on ports `5080` and `5173`.

## Security boundaries

- The browser is untrusted input.
- Role visibility in React is not authorization.
- JWT signing keys and bootstrap keys are deployment secrets.
- The database is not directly exposed to the web client.
- Uploaded files are untrusted and pass API validation before storage.
- Audit metadata must not become a secondary PII store.
- Service-worker cache must never expand to authenticated API responses without a separate privacy design review.

See `SECURITY.md`, `PRIVACY.md`, `THREAT_MODEL.md`, and the ADRs under `docs/adr/`.

## Cross-cutting concerns

### Error handling

The API returns safe problem responses and logs server-side diagnostic context. The Web/PWA renders loading, empty, success, offline, validation, and failure states without exposing stack traces.

### Observability

Structured application logs, correlation/request identifiers, health checks, and audit records are distinct signals. Audit history is a business/security record and must not be treated as a replacement for operational logs.

### Accessibility

Semantic HTML, label association, focus visibility, skip navigation, keyboard-reachable actions, responsive layout, reduced-motion handling, and printable report semantics are baseline requirements. See `docs/accessibility.md`.

### Performance

The architecture favors server-side filtering/pagination, lean JSON contracts, static PWA assets, and narrow page-level data requests. See `docs/performance.md`.

## Adding a module

A new business capability should normally be implemented in this order:

1. model domain concepts and invariants;
2. add application contracts/services;
3. add infrastructure mapping/storage implementation when required;
4. add migration for persistence changes;
5. expose explicitly authorized API endpoints;
6. add Web/PWA workflow and state handling;
7. add unit/integration/UI tests;
8. document security, migration, accessibility, and operational impact.

Avoid creating a new architectural layer solely to increase abstraction count. Prefer a coherent module with clear boundaries.
