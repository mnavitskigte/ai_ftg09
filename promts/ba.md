You are a senior Business Analyst. Create a complete Business Requirements Document
(BRD) and User Story set for a Supplier Data ETL pipeline MVP.

## System Context
- Platform: Azure Durable Functions (.NET 8), SQL Server, Azure-hosted
- Trigger: HTTP webhook callback from supplier system
- Source: SOAP XML web service — full data load per trigger
  (no LastModifiedDate or ChangeToken available from source side)
- Destination: REST API service
- Volume: ~100+ supplier records per load
- All suppliers share the same schema (provided separately)

## Functional Requirements

### F1 — Ingestion
On webhook receipt, the function fetches a full supplier list from the SOAP source.
Change detection is performed internally (no source-side delta support).

### F2 — Validation (pre-processing gate, skip-and-continue)
Each record is validated before proceeding. Invalid records are skipped and logged.
Mandatory validation rules:
- SupplierId uniqueness (within batch and against database)
- Bank details: all mandatory fields present and non-empty
- Address details: all mandatory fields present and non-empty

### F3 — Change Detection
Each valid record is compared against its last persisted snapshot (hash or field-level).
Classification: NEW | UPDATED | UNCHANGED.
Only NEW and UPDATED records (plus PENDING_RETRY from prior runs) are sent to the REST API.

### F4 — Persistence & History
- All validated records persisted to a main Suppliers table.
- Custom audit/shadow table captures full row snapshot on every INSERT and UPDATE.
- Per REST API call per supplier row: request payload, response payload, HTTP status,
  timestamp stored in a dedicated log table.

### F5 — Outbound REST API Integration
NEW, UPDATED, and PENDING_RETRY records are dispatched to the destination REST API.
On failure: record is marked as PENDING_RETRY and included in the next scheduled run.
No same-run retries for REST API failures.

### F6 — KPI Tracking (per ETL run)
Capture: total records processed, error rate (validation + API failures),
p95 latency per run, supplier SLA compliance (% delivered within SLA window),
failed batches count, total processing time, number of retries.

### F7 — Audit Trail
Every ETL run must log: start time, end time, trigger source, records in,
validated, sent, failed, skipped — even if the run fails mid-way.

## Non-Functional Requirements
- System must not crash on partial failures — skip-and-continue is the fallback.
- All credentials stored in Azure Key Vault.
- Structured logging required throughout.

## Deliverables (produce all of the following)
1. BRD — functional and non-functional requirements, assumptions, constraints.
2. Glossary of domain terms (ETL run, supplier record, pending retry, audit snapshot, SLA window).
3. User Stories for three roles: Operations Team, Integration Team, Business Stakeholder.
   Format: "As a [role], I want [feature] so that [benefit]."
4. Acceptance Criteria for each User Story in Given/When/Then format.
5. Step-by-step ETL lifecycle process flow (textual, numbered).
6. Open questions list — do NOT invent business rules; raise ambiguities instead.
   Flag: SLA window definition, REST API authentication type, supplier onboarding process.

Use structured Markdown. Flag every assumption explicitly.