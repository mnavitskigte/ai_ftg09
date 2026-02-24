# Supplier Data ETL Pipeline MVP — BRD and User Stories

## 1) Business Requirements Document (BRD)

### 1.1 Purpose
Define the MVP business requirements for a supplier data ETL pipeline that ingests supplier records from a SOAP source, validates and classifies records, persists state/history, and dispatches eligible records to a destination REST API with operational observability.

### 1.2 Business Objectives
- Ensure reliable supplier data delivery from source to destination with skip-and-continue resilience.
- Provide auditable, run-level and row-level traceability for compliance and support.
- Minimize duplicate/invalid supplier propagation through pre-processing validation.
- Enable operational oversight through KPI tracking and run audit trails.

### 1.3 Scope (MVP)
- Trigger via HTTP webhook callback from supplier system.
- Full supplier load fetched from SOAP XML web service for each trigger.
- Internal change detection (NEW, UPDATED, UNCHANGED).
- Validation gate with skip-and-continue behavior for invalid records.
- Persistence to main supplier data and history/audit storage.
- Outbound REST API dispatch for NEW, UPDATED, and PENDING_RETRY records.
- Run-level KPIs and audit trail collection.

### 1.4 Out of Scope (MVP)
- Source-side delta extraction using LastModifiedDate/ChangeToken.
- Same-run retry for REST API failures.
- Business workflow changes in supplier source onboarding process.
- Advanced analytics capabilities beyond operational dashboard metrics and history views.

### 1.5 Stakeholders
- Operations Team
- Integration Team
- Business Stakeholders (data owners/process owners)

### 1.6 Functional Requirements
#### F1 — Ingestion
- On webhook receipt, system triggers ETL run and fetches full supplier list from SOAP source.
- Change detection is performed internally due to no source delta support.

#### F2 — Validation (Pre-processing Gate; Skip-and-Continue)
- Each supplier record is validated before processing.
- Invalid records are skipped and logged; run continues.
- Mandatory rules:
  - `SupplierId` uniqueness within incoming batch.
  - `SupplierId` uniqueness against persisted supplier records.
  - Required bank fields present and non-empty.
  - Required address fields present and non-empty.

#### F3 — Change Detection
- Each valid record is compared with last persisted snapshot (hash or field-level compare).
- Classification output: `NEW`, `UPDATED`, `UNCHANGED`.
- Only `NEW`, `UPDATED`, and previously `PENDING_RETRY` records are sent outbound.

#### F4 — Persistence & History
- All validated records are persisted to main supplier table.
- Audit/shadow table stores full row snapshots on each INSERT and UPDATE.
- Per outbound REST call per supplier, log request payload, response payload, HTTP status, timestamp.

#### F5 — Outbound REST API Integration
- Dispatch `NEW`, `UPDATED`, and `PENDING_RETRY` records to destination REST API.
- On outbound failure, record status set to `PENDING_RETRY`.
- `PENDING_RETRY` records included in next scheduled run.
- No same-run retry behavior.

#### F6 — KPI Tracking (Per ETL Run)
Capture and persist at run level:
- Total records processed
- Error rate (validation failures + API failures)
- p95 latency per run
- Supplier SLA compliance (% delivered within SLA window)
- Failed batches count
- Total processing time
- Number of retries

#### F7 — Audit Trail
For every ETL run, persist:
- Run start time
- Run end time
- Trigger source
- Records received
- Records validated
- Records sent
- Records failed
- Records skipped
- Logs must be written even if run fails mid-way.

### 1.7 Non-Functional Requirements
- Partial failures must not crash the entire run; skip-and-continue is required fallback.
- Credentials/secrets must be stored in Azure Key Vault.
- Structured logging required throughout the pipeline.

### 1.8 Assumptions (Explicit)
- **Assumption A1:** “Scheduled run” for retry pickup exists in addition to webhook-triggered runs.
- **Assumption A2:** Supplier schema and mandatory bank/address fields are defined and version-controlled outside this document.
- **Assumption A3:** Destination REST API supports idempotent processing or duplicate-safe handling.
- **Assumption A4:** Database stores sufficient historical snapshots to support change detection and audit requirements.
- **Assumption A5:** p95 latency is measured across supplier-level outbound processing events within a run.
- **Assumption A6:** “Failed batches count” refers to run-level grouped processing units defined by implementation.
- **Assumption A7:** Webhook payload has enough metadata to identify trigger source and correlation context.
- **Assumption A8:** Key Vault access and managed identity/RBAC are pre-provisioned for runtime components.

### 1.9 Constraints
- Source system provides full extract only (no native delta markers).
- No same-run retries for outbound REST API failures.
- Processing volume baseline is ~100+ supplier records per load.
- Platform baseline is Azure Durable Functions (.NET 8) with SQL Server storage.

---

## 2) Glossary
- **ETL Run:** A single end-to-end execution instance initiated by trigger that ingests, validates, classifies, persists, and dispatches supplier data.
- **Supplier Record:** One supplier entity payload with required business fields (including bank and address details).
- **Pending Retry (`PENDING_RETRY`):** Status for records that failed outbound API delivery and must be retried in a subsequent run.
- **Audit Snapshot:** Full-row historical copy of supplier data captured on INSERT/UPDATE for traceability.
- **SLA Window:** Maximum allowed elapsed time for successful supplier delivery from accepted processing start to outbound confirmation.

---

## 3) User Stories and Acceptance Criteria (Split by Implementation Layer)

### 3.1 Database (SQL Server)

#### US-DB-01 — Run Audit Persistence (Operations Team)
As an Operations Team member, I want each ETL run to persist start/end and counters in the database so that run health is traceable for support and incident analysis.

**Acceptance Criteria (Given/When/Then)**
- Given an ETL run starts, when persistence begins, then start time and trigger source are written to run audit storage.
- Given a run ends (success or partial failure), when finalization occurs, then end time and counters (in/validated/sent/failed/skipped) are persisted.
- Given a run fails mid-way, when failure handling executes, then partial counters and failure state are still persisted.

#### US-DB-02 — Supplier Main and Snapshot History (Integration Team)
As an Integration Team member, I want validated supplier records and change snapshots stored in SQL so that historical state can be queried for audits and reconciliation.

**Acceptance Criteria (Given/When/Then)**
- Given a validated new supplier, when persistence executes, then supplier row is inserted in main supplier storage and a full-row snapshot is created.
- Given a validated updated supplier, when persistence executes, then supplier row is updated and a new full-row snapshot is created.
- Given an unchanged supplier, when classification is `UNCHANGED`, then no update snapshot is created.

#### US-DB-03 — API Call Logging Storage (Operations Team)
As an Operations Team member, I want each outbound supplier API attempt logged in SQL so that delivery outcomes can be audited.

**Acceptance Criteria (Given/When/Then)**
- Given an outbound supplier call is attempted, when call completes, then request payload, response payload, HTTP status, and timestamp are stored.
- Given outbound call fails, when failure is recorded, then supplier delivery status is persisted as `PENDING_RETRY`.
- Given run-level KPI aggregation executes, when metrics are stored, then retry count includes new `PENDING_RETRY` rows from this run.

#### US-DB-04 — Dashboard Read Model for Job Statistics (Business Stakeholder)
As a Business Stakeholder, I want job execution and performance metrics available from database read views so that dashboards can load statistics quickly and consistently.

**Acceptance Criteria (Given/When/Then)**
- Given ETL runs exist, when dashboard queries run statistics, then each run returns processing time, error rate, p95 latency, SLA compliance, and retry count.
- Given no runs exist for selected period, when dashboard queries statistics, then an empty result is returned without query failure.
- Given failed runs exist, when dashboard queries statistics, then failed batch/run indicators are included.

#### US-DB-05 — Supplier Change History Query Model (Business Stakeholder)
As a Business Stakeholder, I want supplier change history query support in SQL so that dashboard users can inspect how supplier data changed over time.

**Acceptance Criteria (Given/When/Then)**
- Given a supplier has snapshot history, when history is requested by supplier identifier, then snapshots are returned in chronological order with change timestamps.
- Given a supplier has no historical updates, when history is requested, then initial snapshot is returned (if exists) or empty result is returned.
- Given snapshot history is queried, when results are returned, then each row includes changed payload (full snapshot) and metadata needed for audit display.

### 3.2 Backend (Durable Functions + API)

#### US-BE-01 — Full-Load Ingestion Trigger (Integration Team)
As an Integration Team member, I want webhook-triggered full-load ingestion from SOAP so that source data is synchronized using available source capabilities.

**Acceptance Criteria (Given/When/Then)**
- Given a valid webhook callback, when orchestration starts, then full supplier list is requested from SOAP source.
- Given SOAP source returns records, when ingestion completes, then received count is captured in run audit.
- Given SOAP call fails, when error handling executes, then run status reflects failure and details are logged.

#### US-BE-02 — Validation with Skip-and-Continue (Operations Team)
As an Operations Team member, I want invalid supplier records skipped while valid records continue so that one bad record does not terminate the run.

**Acceptance Criteria (Given/When/Then)**
- Given mixed valid/invalid records, when validation executes, then invalid records are skipped and valid records continue.
- Given a record fails validation, when validation completes, then reason and supplier identifier are logged.
- Given run closes, when KPIs are calculated, then validation failures contribute to error rate.

#### US-BE-03 — Change Detection Classification (Integration Team)
As an Integration Team member, I want valid records classified as `NEW`, `UPDATED`, or `UNCHANGED` so that outbound dispatch only includes necessary records.

**Acceptance Criteria (Given/When/Then)**
- Given no prior snapshot for a valid supplier, when classification executes, then record is `NEW`.
- Given prior snapshot with differences, when classification executes, then record is `UPDATED`.
- Given prior snapshot without differences, when classification executes, then record is `UNCHANGED` and excluded from outbound dispatch.

#### US-BE-04 — Outbound Dispatch and Deferred Retry (Integration Team)
As an Integration Team member, I want outbound failures marked `PENDING_RETRY` and retried in later runs so that delivery recovery is controlled and predictable.

**Acceptance Criteria (Given/When/Then)**
- Given dispatch-eligible records (`NEW`, `UPDATED`, prior `PENDING_RETRY`), when outbound processing starts, then records are sent to destination REST API.
- Given outbound success, when response is received, then record status is marked delivered/success.
- Given outbound failure, when error handling executes, then status is set to `PENDING_RETRY` with no same-run retry.

#### US-BE-05 — KPI and Dashboard API Endpoints (Operations Team)
As an Operations Team member, I want backend endpoints for run statistics and performance so that the dashboard can display execution health.

**Acceptance Criteria (Given/When/Then)**
- Given dashboard requests run KPIs, when backend endpoint is called, then response includes processed count, sent, failed, skipped, processing time, error rate, p95 latency, SLA compliance, and retries.
- Given date-range filters are provided, when backend endpoint processes request, then only matching runs are returned.
- Given backend cannot reach KPI storage, when request is handled, then API returns structured error response and logs correlation id.

#### US-BE-06 — Supplier Change History API Endpoint (Business Stakeholder)
As a Business Stakeholder, I want a backend endpoint to retrieve supplier change history so that dashboard users can audit supplier field evolution.

**Acceptance Criteria (Given/When/Then)**
- Given a valid supplier identifier, when history endpoint is called, then backend returns ordered snapshots with timestamps.
- Given supplier identifier is unknown, when history endpoint is called, then backend returns empty dataset (or not found behavior per API contract).
- Given history is returned, when payload is serialized, then fields required for dashboard display are included consistently.

### 3.3 Frontend (Dashboard)

#### US-FE-01 — Job Execution Dashboard View (Operations Team)
As an Operations Team member, I want a dashboard view of ETL job executions so that I can quickly assess run outcomes.

**Acceptance Criteria (Given/When/Then)**
- Given run data exists, when dashboard loads, then a list/table of runs is displayed with run status and key counters.
- Given a run is selected, when details are expanded or opened, then that run’s metrics and timing are shown.
- Given backend returns no data, when dashboard loads, then an empty-state message is shown.

#### US-FE-02 — Performance Statistics Visualization (Business Stakeholder)
As a Business Stakeholder, I want dashboard statistics for ETL performance so that I can evaluate operational performance and SLA behavior.

**Acceptance Criteria (Given/When/Then)**
- Given run KPI data is available, when statistics widgets render, then processing time, error rate, p95 latency, SLA compliance, failed batches, and retries are shown.
- Given a date range is selected, when dashboard refreshes, then statistics update to match selected period.
- Given KPI retrieval fails, when dashboard handles error, then a user-visible error state is shown and UI remains responsive.

#### US-FE-03 — Supplier Change History View (Business Stakeholder)
As a Business Stakeholder, I want to view supplier change history in the dashboard so that I can understand what changed and when for a supplier.

**Acceptance Criteria (Given/When/Then)**
- Given a supplier is selected, when history view is opened, then ordered change snapshots are displayed with timestamps.
- Given multiple snapshots exist, when user reviews the timeline/list, then the latest change is clearly identifiable.
- Given no history exists for supplier, when history view is opened, then a no-history state is displayed.

#### US-FE-04 — Drill-Down from Job to Supplier Failures (Operations Team)
As an Operations Team member, I want to drill from a failed run to impacted suppliers so that I can identify actionable records.

**Acceptance Criteria (Given/When/Then)**
- Given a run has failed/skipped records, when user opens run details, then affected supplier identifiers and failure categories are displayed.
- Given supplier failure is `PENDING_RETRY`, when details are shown, then retry status is visible.
- Given no failed records in a run, when details are viewed, then failure drill-down section indicates no failures.

---

## 4) ETL Lifecycle Process Flow (Textual, Numbered)
1. Receive HTTP webhook callback from supplier system.
2. Initialize ETL run context and write run start audit entry.
3. Request and retrieve full supplier list from SOAP XML source.
4. Record inbound count and correlation metadata.
5. For each supplier record, execute validation gate.
6. If validation fails, log reason, increment skipped/error counters, continue to next record.
7. For valid record, compare with last persisted snapshot.
8. Classify as `NEW`, `UPDATED`, or `UNCHANGED`.
9. Persist validated record in main suppliers table as applicable.
10. On INSERT/UPDATE, write full-row snapshot to audit/shadow table.
11. Build outbound queue containing `NEW`, `UPDATED`, and prior `PENDING_RETRY` records.
12. Send each queued record to destination REST API.
13. For each outbound call, log request payload, response payload, HTTP status, timestamp.
14. On outbound failure, mark record `PENDING_RETRY` (no same-run retry).
15. Aggregate run KPIs (processed count, error rate, p95 latency, SLA compliance, failed batches, processing time, retries).
16. Finalize run with end audit entry and counters (including partial completion if failures occurred).

---

## 5) Open Questions (Ambiguities to Resolve)
1. **SLA Window Definition (Flagged):** What exact start/end events define SLA timing (webhook receive, validation pass, API success response, or other)?
2. **REST API Authentication Type (Flagged):** Which auth mechanism is required (OAuth2 client credentials, mTLS, API key, managed identity, etc.)?
3. **Supplier Onboarding Process (Flagged):** How are new suppliers provisioned, validated, and activated before first ETL ingestion?
4. What is the exact list of mandatory bank and address fields per supplier schema version?
5. What is the duplicate-handling rule when `SupplierId` exists in DB but source payload differs on immutable identity attributes?
6. Should `UNCHANGED` records be persisted as run-observed events, or only tracked via counters?
7. What are target timeout, throttle, and rate-limit policies for outbound REST API calls?
8. How should “failed batches count” be defined operationally (per run chunk, per API batch, per orchestration activity)?
9. What is retention policy for audit snapshots and API payload logs, including PII/data minimization constraints?
10. What is expected retry cadence and maximum retry horizon for `PENDING_RETRY` records?
