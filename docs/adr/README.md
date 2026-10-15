# Architecture Decision Records

CampusCore uses Architecture Decision Records (ADRs) for decisions that materially affect maintainability, security, data compatibility, deployment, or contributor expectations.

## Status vocabulary

- **Proposed** — under active review and not yet the repository standard.
- **Accepted** — current architectural direction.
- **Superseded** — replaced by a newer ADR; retained for historical context.
- **Deprecated** — still visible for compatibility but should not be used for new work.

## ADR index

| ADR | Decision | Status |
| --- | --- | --- |
| [0001](0001-modular-monolith-clean-boundaries.md) | Modular monolith with explicit Domain/Application/Infrastructure/API boundaries | Accepted |
| [0002](0002-postgresql-and-ef-core-migrations.md) | PostgreSQL with EF Core migrations as schema history | Accepted |
| [0003](0003-react-pwa-and-api-deployment-boundary.md) | React/Vite PWA deployed separately from the ASP.NET Core API | Accepted |
| [0004](0004-server-enforced-jwt-role-authorization.md) | Server-enforced JWT authentication and role authorization | Accepted |
| [0005](0005-attachment-storage-abstraction.md) | Attachment storage through an application abstraction with bounded local storage initially | Accepted |

## Creating a new ADR

Use the next four-digit number and a short kebab-case title. Each ADR should include:

1. status and date;
2. context and forces;
3. decision;
4. consequences, including trade-offs;
5. alternatives considered;
6. conditions that would trigger a review or superseding ADR.

Do not silently rewrite an accepted ADR when the architecture changes. Create a new ADR and mark the older record as superseded.
