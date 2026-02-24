You are a senior .NET 8 architect. Scaffold a complete Azure Durable Functions ETL
pipeline solution for supplier data processing. Generate project structure, all
interfaces, class stubs with XML doc comments, SQL migration scripts, and a README.

## Architecture

Trigger:       HTTP Trigger (webhook from supplier) → starts Durable Orchestrator
Orchestrator:  Azure Durable Function (fan-out/fan-in pattern for parallel processing)
Source:        SOAP XML web service (WSDL-based; schema injected separately)
Destination:   REST API (typed HttpClient; schema injected separately)
Storage:       Azure SQL Server via EF Core or Dapper
Config:        Azure App Settings + Azure Key Vault (no hardcoded values)
Logging:       Serilog structured logging via ILogger throughout

## Pipeline Steps (implement in this order)

1.  Webhook received → create new EtlRun record (status: Running), log trigger source.
2.  Call SOAP XML source → fetch full supplier list (~100+ records).
3.  Deserialize XML → map to internal SupplierRecord domain model.
4.  Per-record validation (skip invalid, log reason to SupplierValidationErrors):
    - SupplierId uniqueness within batch
    - Bank details mandatory fields non-empty
    - Address details mandatory fields non-empty
5.  Change detection: compare each valid record hash against last DB snapshot.
    Classify: NEW | UPDATED | UNCHANGED.
6.  Persist all valid records to Suppliers table.
    Write shadow/audit rows to SuppliersAudit for NEW and UPDATED only.
7.  Collect PENDING_RETRY records from SupplierPendingRetry table.
    Merge with NEW + UPDATED records for dispatch queue.
8.  Fan-out: dispatch each record to destination REST API (parallel activity functions).
    Per call: persist full request JSON, response JSON, HTTP status, timestamp, IsSuccess
    to SupplierApiCallLog.
    On failure: upsert record in SupplierPendingRetry (increment RetryCount).
    On success: delete record from SupplierPendingRetry if it existed.
9.  Fan-in: collect all results.
10. Compute and persist KPIs to EtlRuns:
    - ErrorRate = (InvalidRecords + ApiFailures) / TotalRecords
    - P95LatencyMs = 95th percentile of per-record processing durations
    - SlaCompliantCount = records delivered within [SLA window — configurable]
    - TotalDurationMs, RetryCount, FailedBatches
11. Update EtlRun status to Completed (or PartialFailure if any API failures).

## Database Schema — generate migration scripts for all tables

### Suppliers
SupplierId, Name, [all supplier fields — schema TBD], RowHash nvarchar(64),
CreatedAt, UpdatedAt, LastRunId

### SuppliersAudit (shadow table)
AuditId, SupplierId, RunId, ChangeType (INSERT|UPDATE), ChangedAt,
[full snapshot of all supplier fields]

### SupplierApiCallLog
Id, SupplierId, RunId, RequestPayload nvarchar(max), ResponsePayload nvarchar(max),
HttpStatusCode int, CalledAt datetime2, IsSuccess bit, DurationMs bigint

### EtlRuns
RunId, StartedAt, CompletedAt, TriggerSource, Status (Running|Completed|PartialFailure|Failed),
TotalRecords, ValidRecords, InvalidRecords, NewRecords, UpdatedRecords, UnchangedRecords,
SentToApi, ApiFailures, RetryCount, ErrorRate decimal(5,4),
P95LatencyMs bigint, TotalDurationMs bigint, SlaCompliantCount

### SupplierValidationErrors
Id, RunId, RawSupplierId nvarchar(100), ErrorReason nvarchar(500), OccurredAt

### SupplierPendingRetry
Id, SupplierId, OriginalRunId, FailedAt, RetryCount int, LastErrorMessage nvarchar(1000)

## Service Classes to Generate

| Class                     | Responsibility                                              |
|---------------------------|-------------------------------------------------------------|
| SupplierEtlOrchestrator   | Durable orchestrator — pipeline coordination                |
| WebhookHttpTrigger        | HTTP trigger entry point, run initialization                |
| SoapSourceClient          | ISoapSourceClient — typed WSDL SOAP caller                  |
| SupplierValidator         | ISupplierValidator — FluentValidation rules                 |
| ChangeDetectionService    | IChangeDetectionService — SHA256 hash comparison            |
| SupplierRepository        | ISupplierRepository — all DB reads/writes                   |
| DestinationApiClient      | IDestinationApiClient — typed HttpClient with Polly timeout |
| EtlMetricsService         | IEtlMetricsService — KPI calculation and persistence        |
| AuditService              | IAuditService — shadow table + API call log writes          |
| RetryQueueService         | IRetryQueueService — pending retry management               |

## Non-Functional Constraints
- All async methods accept CancellationToken.
- All external dependencies injected via interfaces (testable).
- Transactions wrapping Suppliers + SuppliersAudit writes.
- Polly used on DestinationApiClient for timeout and transient HTTP errors only.
  Business retry logic lives in RetryQueueService, not Polly.
- SOAP credentials and REST API keys from Key Vault references in App Settings.
- No cold-start issues: use Premium plan or pre-warmed instances.

## Generate
1. Solution and project folder structure (src/, tests/, infra/).
2. All interface definitions with XML doc comments.
3. All class stubs (constructor, method signatures, XML doc comments, no implementation).
4. SQL migration scripts (numbered, e.g., 001_InitialSchema.sql).
5. local.settings.json template with all required keys (values as placeholders).
6. README.md describing pipeline flow, local dev setup, and deployment notes.

Do not implement business logic yet — stubs and interfaces only.
Mark every schema-dependent placeholder with // TODO: inject schema.
