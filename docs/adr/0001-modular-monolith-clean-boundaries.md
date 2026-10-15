# ADR 0001 — Modular Monolith with Clean Boundaries

- Status: Accepted
- Date: 2026-08-22

## Context

CampusCore contains several related school-management capabilities: student records, guardians, enrollment, attendance, marks, staff, timetable, announcements, reporting, audit, settings, and administration. They share one consistency boundary and are commonly deployed together.

Splitting these capabilities into independent services at the current scale would add network contracts, distributed transactions, deployment coordination, observability overhead, and more failure modes without a demonstrated scaling requirement.

At the same time, putting all logic directly into HTTP endpoints would make tests, business rules, and future evolution harder.

## Decision

CampusCore is a **modular monolith** with explicit dependency direction:

```text
CampusCore.Domain
      ↑
CampusCore.Application
      ↑
CampusCore.Infrastructure
      ↑
CampusCore.Api

CampusCore.Web → HTTP API contracts
```

The practical rules are:

- `CampusCore.Domain` owns entities, value rules, and domain enums. It has no dependency on ASP.NET Core, EF Core, PostgreSQL, or the Web client.
- `CampusCore.Application` owns use cases, request/response models, interfaces/ports, orchestration, and business-facing validation that requires repositories or services.
- `CampusCore.Infrastructure` implements persistence, identity integration, auditing, and storage ports.
- `CampusCore.Api` is the HTTP composition root. Endpoints translate HTTP concerns to application calls and enforce authentication/authorization.
- `CampusCore.Web` consumes supported HTTP contracts and never becomes the authority for permissions or business invariants.

New capabilities should remain cohesive modules inside these boundaries. Cross-module access should use application interfaces or shared domain concepts rather than reaching into another module's persistence implementation.

## Consequences

### Positive

- One deployable backend keeps transactions and operations understandable.
- Domain and application behavior can be tested without an HTTP server.
- Infrastructure can evolve behind interfaces.
- Feature code remains discoverable in one repository and one solution.
- Future extraction of a genuinely independent capability remains possible because boundaries are explicit.

### Trade-offs

- A modular monolith still requires discipline; project boundaries do not automatically prevent all coupling.
- One backend release can affect multiple capabilities, so CI and regression coverage are important.
- Scaling is primarily vertical/horizontal at the application level until a proven hotspot justifies different architecture.

## Alternatives considered

### Microservices now

Rejected because operational complexity and distributed consistency costs exceed current demonstrated benefit.

### Single ASP.NET Core project with endpoint-centric logic

Rejected because it would couple transport, persistence, and business rules, making tests and future changes harder.

### Separate repository per module

Rejected because the product is currently developed and released as one coherent system; multi-repository coordination would create unnecessary friction.

## Review triggers

Revisit this ADR when one or more of these conditions are measured rather than assumed:

- a capability needs a materially different scaling profile;
- independent release cadence becomes an organizational requirement;
- data ownership can be cleanly separated with acceptable consistency semantics;
- compliance or isolation requirements demand a stronger process/data boundary;
- the modular monolith shows persistent coupling that cannot reasonably be corrected within the existing architecture.
