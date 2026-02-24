# .NET / Backend Role (C# .NET 8)

## Responsibilities
- Develop and maintain the .NET 8 Web API (`backend/api/`) that exposes ETL job data.
- Develop and maintain the Azure Functions project (`backend/etl-function/`) that executes the ETL pipeline on a cron schedule.
- Write unit and integration tests (xUnit).
- Ensure proper error handling, logging (structured via `ILogger<T>`), and observability (Application Insights).
- Manage NuGet package versions and avoid vulnerable dependencies.
- Participate in code reviews for all C# changes.

## Key Files
| Path | Purpose |
|------|---------|
| `backend/api/EtlApi.csproj` | Web API project (net8.0) |
| `backend/api/Program.cs` | Minimal API endpoints |
| `backend/api/appsettings.json` | App configuration |
| `backend/etl-function/EtlFunction.csproj` | Azure Functions project (isolated, net8.0) |
| `backend/etl-function/Functions/RunEtlFunction.cs` | Timer-triggered ETL runner |
| `backend/etl-function/Program.cs` | Functions host builder |
| `backend/etl-function/host.json` | Functions host settings |
| `backend/etl-function/local.settings.json` | Local dev settings (not committed to prod) |

## Standards & Conventions
- Target: .NET 8, C# 12, nullable reference types enabled.
- Use `async/await` throughout; never `.Result` or `.Wait()`.
- Use the `IConfiguration` / options pattern for all settings; no hard-coded values.
- Use Dapper package for Azure SQL.
- Azure Managed Identity is the preferred auth method for Azure SQL.
- Log structured messages via `ILogger<T>` using message templates, not string interpolation.
- Cron format for Azure Functions timer triggers: `{second} {minute} {hour} {day} {month} {day-of-week}`.
- Package versions must be pinned; update only via reviewed PRs.
- All public API surface must have Swagger/OpenAPI annotations.
