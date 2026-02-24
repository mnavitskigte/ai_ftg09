# ai_ftg09
Copilot training project

## ETL Platform – Project Structure

```
ai_ftg09/
├── db/                          # Azure MS SQL database project (SSDT)
│   ├── EtlDatabase.sqlproj
│   └── Schema/
│       ├── Tables/              # EtlJob, EtlJobLog
│       ├── StoredProcedures/    # usp_StartEtlJob, usp_LogEtlJobResult
│       └── Views/               # vw_EtlJobStatus
│
├── backend/
│   ├── api/                     # .NET 8 Minimal Web API (EtlApi)
│   │   ├── EtlApi.csproj
│   │   └── Program.cs           # GET /api/etl-jobs, GET /api/etl-jobs/{id}/logs
│   └── etl-function/            # Azure Functions v4 isolated (net8.0)
│       ├── EtlFunction.csproj
│       ├── Program.cs
│       ├── host.json
│       ├── local.settings.json  # local dev only – not committed
│       └── Functions/
│           └── RunEtlFunction.cs  # Timer trigger (cron: daily 02:00 UTC)
│
├── frontend/                    # Angular 21 dashboard (etl-dashboard)
│   └── src/
│       ├── app/
│       │   ├── etl-jobs/        # ETL jobs feature component
│       │   └── services/        # EtlJobsService (HTTP)
│       └── environments/        # environment.ts / environment.prod.ts
│
└── .github/
    ├── mcp.json                 # MCP server config (angular-cli, dotnet, mssql)
    └── skills/
        ├── db-role.md
        ├── dotnet-role.md
        ├── angular-frontend-role.md
        └── business-analyst-role.md
```

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Node.js 20+](https://nodejs.org/) & Angular CLI (`npm i -g @angular/cli`)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [SQL Server Data Tools (SSDT)](https://learn.microsoft.com/sql/ssdt/download-sql-server-data-tools-ssdt) or `sqlpackage`
- An Azure SQL Database (or local SQL Server for development)

### Database
```bash
cd db
# Deploy via sqlpackage or open EtlDatabase.sqlproj in Visual Studio / Azure Data Studio
```

### Backend API
```bash
cd backend/api
dotnet restore
dotnet run
# Swagger UI: https://localhost:5001/swagger
```

### Azure Function (ETL Runner)
```bash
cd backend/etl-function
# Copy local.settings.json and fill in SqlConnectionString
dotnet restore
func start
```

### Frontend
```bash
cd frontend
npm install
ng serve
# App: http://localhost:4200
```

## MCP Servers
The `.github/mcp.json` file registers three MCP servers for AI-assisted development:
- **angular-cli** – Angular code generation & migration
- **dotnet** – .NET project tooling
- **mssql** – Natural-language SQL against the Azure SQL database
