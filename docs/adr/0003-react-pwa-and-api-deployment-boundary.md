# ADR 0003 — React PWA and API Deployment Boundary

- Status: Accepted
- Date: 2026-08-22

## Context

CampusCore needs a responsive Web/PWA experience while keeping authentication, authorization, validation, persistence, auditing, and sensitive operations authoritative on the server.

The browser client benefits from an independent frontend toolchain and static deployment. The ASP.NET Core API benefits from remaining focused on HTTP/API and application composition instead of also owning the frontend build system.

## Decision

CampusCore uses a **React + TypeScript + Vite Web/PWA** as a separately built static application and an **ASP.NET Core API** as the server authority.

Deployment rules:

- the frontend is compiled to static assets;
- the production Web image serves those assets through a hardened nginx configuration;
- browser API access uses an explicitly configured public API base URL;
- CORS is allowlisted by deployment configuration;
- the frontend may improve usability through role-aware presentation, but the API always enforces authorization;
- service-worker caching focuses on application-shell reliability unless authenticated-data caching receives a separate privacy/correctness design review;
- API contracts are treated as stable product boundaries and should evolve compatibly where reasonable.

Docker Compose can run Web, API, and PostgreSQL together for local or self-hosted evaluation while keeping the logical deployment boundaries intact.

## Consequences

### Positive

- frontend and backend can build, test, and package independently;
- static frontend hosting is simple and cache-friendly;
- API authorization remains independent of browser implementation details;
- the PWA can support installation and shell-level offline behavior without coupling server execution to the UI build;
- backend-only consumers can use the HTTP API in the future.

### Trade-offs

- deployments must configure the browser-visible API URL and allowed origins correctly;
- compatibility between independently built frontend and API artifacts needs release discipline;
- two production images add some deployment configuration compared with serving everything from one process.

## Alternatives considered

### Serve the React build directly from ASP.NET Core

Viable for a smaller deployment, but rejected as the primary architecture because it couples frontend packaging to the API artifact and reduces deployment flexibility.

### Server-rendered UI only

Rejected because the product requires a rich responsive administration interface and installable PWA experience.

### Cache authenticated API data broadly in the service worker

Rejected as a default. Sensitive education data needs explicit account partitioning, retention, invalidation, and offline-consistency semantics before such caching is safe.

## Review triggers

Revisit when:

- a single-artifact deployment becomes an important supported distribution mode;
- server-side rendering becomes necessary for a measured product requirement;
- offline authenticated workflows are formally designed and require a different data architecture;
- API versioning or multi-client compatibility needs a stronger contract-management system.
