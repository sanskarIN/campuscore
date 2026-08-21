# Privacy

CampusCore is self-hosted software. The deploying institution controls the database, accounts, retention, backups, and legal basis for processing student and staff records.

CampusCore does not require telemetry to function. Application logs are designed to avoid passwords, tokens, authorization headers, document bodies, and unnecessary PII. Audit events record actor, action, entity type, entity identifier, timestamp, and safe metadata rather than full sensitive records.

Operators should configure TLS, least-privilege database credentials, retention, backups, access roles, and applicable consent/notice obligations before production use. Demo seed data is fictional only.
