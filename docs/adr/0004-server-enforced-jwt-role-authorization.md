# ADR 0004 — Server-Enforced JWT and Role Authorization

- Status: Accepted
- Date: 2026-08-22

## Context

CampusCore contains sensitive education and administration data. Users may have different duties, and the browser is an untrusted execution environment. Hiding a button in React cannot establish permission to read or mutate server data.

The current API uses ASP.NET Core Identity integration and JWT bearer authentication. Administrative endpoints require role-aware authorization.

## Decision

CampusCore uses **server-validated JWT bearer authentication** and **server-enforced authorization policies/roles** for protected HTTP operations.

Security rules:

- the API validates token issuer, audience, lifetime, and signing key;
- the signing key is deployment secret material and must never be committed;
- production signing keys must have adequate entropy and be supplied through an approved secret store;
- API endpoint authorization is authoritative for every protected operation;
- the Web/PWA may use role information only to improve navigation and action discoverability;
- a client-visible role or UI state never grants permission by itself;
- authentication failures use user-safe responses that do not disclose whether an account exists;
- sensitive account/role changes are audited;
- token/cookie/authorization-header values must not be written to general application logs.

Short-lived bearer tokens are preferred over indefinitely valid credentials. Any future refresh-token mechanism requires explicit rotation, revocation, replay, storage, and account-disable semantics.

## Consequences

### Positive

- authorization remains trustworthy even if a user modifies browser code or calls the API directly;
- standard ASP.NET Core authentication/authorization primitives reduce custom security code;
- API routes can be reviewed independently of frontend presentation;
- role changes and privileged actions can be centrally audited.

### Trade-offs

- bearer tokens require careful browser/session handling and expiration UX;
- long sessions may eventually require a reviewed refresh-token design;
- role-based authorization can become too coarse if fine-grained institution policies are introduced later.

## Alternatives considered

### Frontend-only permissions

Rejected because client code is user-controlled and cannot protect server resources.

### Custom authentication/cryptography

Rejected in favor of maintained framework primitives.

### Long-lived static API tokens for interactive users

Rejected because revocation, compromise impact, and user-session management are worse for the primary browser workflow.

## Review triggers

Create a superseding ADR if CampusCore adds:

- refresh tokens or server-managed browser sessions;
- multi-institution tenancy with scoped roles;
- attribute/policy-based permissions beyond current roles;
- external identity providers or SSO;
- mobile/native clients with materially different credential-storage requirements.
