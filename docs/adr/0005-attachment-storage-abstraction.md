# ADR 0005 — Attachment Storage Abstraction

- Status: Accepted
- Date: 2026-08-22

## Context

CampusCore announcements can include document attachments. Files are different from ordinary relational rows: they may be large, require streaming, need deployment-specific persistence, and must not expose arbitrary filesystem paths.

A direct dependency from endpoint code to one disk path would make deployment changes harder and would spread path-security concerns through the application.

## Decision

CampusCore stores attachment metadata in the relational model while file bytes are accessed through the application `IFileStorage` abstraction.

The initial infrastructure implementation is `LocalFileStorage`, which:

- writes beneath a configured storage root;
- generates opaque random stored names rather than trusting user filenames;
- normalizes and bounds extensions;
- rejects names that contain path components;
- verifies resolved paths remain beneath the configured root;
- streams reads and writes asynchronously;
- creates files with `CreateNew` to avoid accidental overwrite.

HTTP upload endpoints remain responsible for permitted file types, request-size limits, authorization, metadata validation, and safe download response headers. The storage adapter is not a malware scanner and does not infer trust from an extension.

Production deployments using local storage must mount the configured root on persistent storage and include it in backup/capacity planning.

## Consequences

### Positive

- application use cases do not depend on a specific filesystem or cloud provider;
- path traversal defenses are centralized;
- opaque storage names prevent user-controlled filenames from becoming paths;
- a future object-storage adapter can implement the same port without changing domain behavior.

### Trade-offs

- local storage requires durable volume configuration in multi-container deployments;
- horizontal API replicas need shared storage or an object-storage implementation;
- file metadata and bytes are separate resources, so failed multi-step operations need cleanup and reconciliation discipline.

## Alternatives considered

### Store attachment bytes in PostgreSQL

Rejected as the default because large file traffic would increase database size, backup cost, and I/O pressure. Small binary storage could be reconsidered for a different workload, but it is not the current design.

### Allow endpoints to write directly to arbitrary paths

Rejected because it couples transport code to deployment details and broadens the path-security surface.

### Adopt cloud object storage immediately

Deferred because local persistent storage is sufficient for the current self-hosted architecture and keeps development simple. The abstraction preserves a future migration path.

## Review triggers

Revisit when:

- CampusCore runs multiple API replicas without a shared filesystem;
- attachment volume or retention exceeds practical local-volume operation;
- malware scanning, content-disarm/reconstruction, legal hold, or immutable retention becomes required;
- signed external download URLs become a product requirement;
- backup/recovery objectives require provider-managed object storage.
