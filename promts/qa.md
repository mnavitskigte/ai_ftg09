You are a senior QA engineer. Generate a comprehensive test suite for a Supplier ETL
pipeline built on Azure Durable Functions (.NET 8) with SQL Server.

## System Under Test Summary
- HTTP webhook → Durable Orchestrator → SOAP fetch → validate → change detect
  → persist → dispatch to REST API → log KPIs
- Fallback: skip invalid records, continue processing
- Retry: failed REST API calls included in next run (not same-run retry)
- Volume: ~100+ supplier records per run
- DB tables: Suppliers, SuppliersAudit, SupplierApiCallLog, EtlRuns,
             SupplierValidationErrors, SupplierPendingRetry

## Test Categories

### UNIT TESTS (xUnit + FluentAssertions + Moq)

**SupplierValidator**
- TC-U-01: All mandatory fields populated → validation passes
- TC-U-02: BankAccountNumber empty → validation fails, reason logged
- TC-U-03: BankIBAN null → validation fails
- TC-U-04: AddressLine1 empty → validation fails
- TC-U-05: AddressCity null → validation fails
- TC-U-06: AddressCountry empty → validation fails
- TC-U-07: Duplicate SupplierId in same batch → validation fails for duplicate
- TC-U-08: SupplierId already in DB → validation fails (uniqueness)
- TC-U-09: All fields valid, unique id → passes all rules

**ChangeDetectionService**
- TC-U-10: Record not in DB → classified as NEW
- TC-U-11: Record hash matches DB snapshot → classified as UNCHANGED
- TC-U-12: Any single field changed → classified as UPDATED
- TC-U-13: Two different records produce different hashes (no collision)
- TC-U-14: Null field in record handled without exception

**EtlMetricsService**
- TC-U-15: ErrorRate = 0 when all records valid and all API calls succeed
- TC-U-16: ErrorRate = 1 when all records fail validation
- TC-U-17: P95LatencyMs correctly computed from a known list of durations
- TC-U-18: SlaCompliantCount = total when all records within SLA window
- TC-U-19: SlaCompliantCount = 0 when all records exceed SLA window
- TC-U-20: TotalDurationMs = CompletedAt - StartedAt in milliseconds

### INTEGRATION TESTS (TestContainers or LocalDB)

**SupplierRepository**
- TC-I-01: InsertSupplier → row present in Suppliers with correct fields
- TC-I-02: UpdateSupplier → SuppliersAudit row inserted with ChangeType = UPDATE
- TC-I-03: InsertSupplier → SuppliersAudit row inserted with ChangeType = INSERT
- TC-I-04: GetPendingRetries → returns all rows from SupplierPendingRetry
- TC-I-05: DeletePendingRetry → row removed after successful API call
- TC-I-06: IncrementRetryCount → RetryCount + 1, LastErrorMessage updated

**AuditService**
- TC-I-07: Successful API call → SupplierApiCallLog row with IsSuccess = true
- TC-I-08: Failed API call → SupplierApiCallLog row with IsSuccess = false,
            correct HTTP status code

### END-TO-END / PIPELINE TESTS (mock SOAP and REST endpoints via WireMock.Net)

**Happy Path**
- TC-E-01: Webhook → SOAP returns 100 valid records → all pass validation →
           50 NEW + 50 UPDATED → all REST calls succeed →
           EtlRuns.SentToApi = 100, SupplierPendingRetry empty, status = Completed

**Validation Skip-and-Continue**
- TC-E-02: 10 records fail validation (e.g., missing bank details) → 90 processed →
           SupplierValidationErrors has 10 rows → EtlRuns.InvalidRecords = 10 →
           run still completes

**Retry Inclusion Across Runs**
- TC-E-03: Run 1: 5 API failures → SupplierPendingRetry has 5 rows, RetryCount = 1
- TC-E-04: Run 2: 5 retries included → 2 succeed → 3 fail again →
           SupplierPendingRetry has 3 rows, RetryCount = 2

**Change Detection Full Load**
- TC-E-05: Run 1: 100 records → all NEW → REST API called 100 times
- TC-E-06: Run 2: same 100 records, no changes → all UNCHANGED →
           REST API called 0 times (excluding pending retries)
- TC-E-07: Run 3: 10 records changed → REST API called 10 times + pending retries

**Total REST API Unavailability**
- TC-E-08: REST API returns 503 for all calls → all 100 records in SupplierPendingRetry →
           EtlRuns.ApiFailures = 100 → run status = PartialFailure → no crash

**SOAP Source Timeout**
- TC-E-09: SOAP service times out → EtlRuns status = Failed →
           no partial writes committed → error logged with run context

**Idempotency**
- TC-E-10: Webhook fired twice simultaneously → second invocation either queued
           or rejected with 409 → no duplicate EtlRun created

### KPI & AUDIT ACCURACY TESTS
- TC-K-01: EtlRuns row always written even if run fails mid-pipeline
- TC-K-02: P95LatencyMs in EtlRuns matches manual percentile of SupplierApiCallLog durations
- TC-K-03: SuppliersAudit has exactly 1 row per INSERT and 1 per UPDATE (no duplicates)
- TC-K-04: RequestPayload and ResponsePayload in SupplierApiCallLog are not truncated
- TC-K-05: ErrorRate decimal precision correct to 4 decimal places

### EDGE CASES & NON-FUNCTIONAL
- TC-N-01: SOAP returns 0 records → run completes gracefully, TotalRecords = 0
- TC-N-02: 500 records in a single load → no timeout, P95 within configurable threshold
- TC-N-03: Missing Key Vault reference → function startup fails with clear error (not silent)

## Deliverables
1. Test Plan document (scope, in/out-of-scope, entry/exit criteria, environments).
2. Test case table for each category:
   Columns: ID | Name | Preconditions | Steps | Expected Result | Priority (P1/P2/P3)
3. xUnit test class stubs — method names + Arrange/Act/Assert comment blocks only.
4. Test data requirements: list of mock SOAP response fixtures and REST API mock
   scenarios needed (happy path, partial failure, timeout, 503).
5. Definition of Done checklist for QA MVP sign-off.

Use structured Markdown. Flag any missing schema details as open questions.
Do not invent acceptance criteria beyond what is stated.
