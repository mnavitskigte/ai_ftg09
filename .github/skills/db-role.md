# Database Role (Azure MS SQL)

## Responsibilities
- Design and maintain the Azure SQL database schema for the ETL platform.
- Write and optimise T-SQL stored procedures, views and indexes.
- Manage database migrations and version-controlled SQL scripts.
- Monitor query performance and implement query-store / execution-plan optimisations.
- Enforce row-level security and data-masking policies where required.
- Maintain the `db/` SQL project (`.sqlproj`) and keep it deployable to Azure SQL via SSDT or Azure DevOps.

## Key Files
| Path | Purpose |
|------|---------|
| `db/EtlDatabase.sqlproj` | SQL Server Data Tools project |
| `db/Schema/Tables/EtlJob.sql` | ETL job registry table |
| `db/Schema/Tables/EtlJobLog.sql` | ETL execution history |
| `db/Schema/StoredProcedures/usp_StartEtlJob.sql` | Start a job run |
| `db/Schema/StoredProcedures/usp_LogEtlJobResult.sql` | Record job outcome |
| `db/Schema/Views/vw_EtlJobStatus.sql` | Operational status view |

## Standards & Conventions
- All objects live in the `[dbo]` schema unless a dedicated schema is justified.
- Use `SYSUTCDATETIME()` for all timestamps (UTC everywhere).
- Every stored procedure must have `SET NOCOUNT ON` and handle `NULL` parameters safely.
- Indexes must be reviewed with the query workload before addition.
- Never use `SELECT *` in views or procedures.
- Use `NVARCHAR` for all string columns; size to realistic maximums.
- Use `DATETIME2(7)` (not `DATETIME`) for date/time columns.
- Schema changes go through a PR review and are script-tested in a dev database first.

## Azure SQL Notes
- Target: `Microsoft.Data.Tools.Schema.Sql.SqlAzureV12DatabaseSchemaProvider`
- Connectivity: use Managed Identity / Active Directory Default auth (no SQL passwords).
- Connection strings must never be committed; use Azure Key Vault references.
