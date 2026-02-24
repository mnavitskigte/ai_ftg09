# Supplier ETL Backend (Durable Functions)

## Overview

This backend scaffold provides Azure Durable Functions orchestration for supplier ETL with:
- HTTP webhook trigger
- Fan-out/fan-in dispatch pipeline
- SOAP source integration abstraction
- Destination REST API abstraction
- SQL repository abstractions for run audit, snapshots, API logs, validation logs, and retry queue

All business logic is intentionally left as stubs (`NotImplementedException`) per scaffold requirement.

## Pipeline Flow (Scaffolded)

1. Webhook trigger receives callback and starts orchestration.
2. Orchestrator starts ETL run record.
3. Fetch full supplier list from SOAP source.
4. Validate records (skip invalid and log).
5. Classify changes (`NEW`, `UPDATED`, `UNCHANGED`).
6. Persist suppliers and write audit snapshots for new/updated.
7. Load `PENDING_RETRY` records and merge with dispatch candidates.
8. Fan-out dispatch to destination API with per-call logging.
9. Fan-in dispatch results.
10. Compute KPIs (error rate, p95, SLA compliant, duration, retries).
11. Complete ETL run with `Completed` or `PartialFailure` status.

## Project Structure

- `etl-function/` Azure Functions v4 isolated worker project with durable orchestrator and service stubs
- `infra/migrations/` numbered SQL migration scripts
- `src/` canonical source layout marker
- `tests/` test project placeholder

## Configuration

Use `etl-function/local.settings.json` for local placeholders:
- SQL connection string
- SOAP endpoint, credentials, action, and operation contract settings (Key Vault references in hosted env)
- Destination API settings (Key Vault references in hosted env)
- SLA window settings

## Local Development

```bash
cd backend/etl-function
dotnet restore
func start
```

## Deployment Notes

- Host on Azure Functions Premium plan or pre-warmed instances to reduce cold start.
- Store secrets in Azure Key Vault and expose via app settings references.
- Use Managed Identity for SQL access in Azure-hosted environment.
- Keep transport retries in Polly; business retries in retry queue service.
