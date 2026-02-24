# Skill Routing Guide

Use this file to select the correct skill document for a given task.

## Primary Routing Matrix

| Task Type | Use Skill | File | Notes |
|---|---|---|---|
| Requirements, BRD, user stories, acceptance criteria, glossary, KPI/SLA definitions | Business Analyst | `.github/skills/business-analyst-role.md` | Start here for scope, process, and story definition work. |
| SQL schema, tables/views/SPs, indexing, SQL performance, data model changes | Database | `.github/skills/db-role.md` | Use for anything under `db/` or SQL persistence/query design. |
| .NET 8 API endpoints, Azure Functions orchestration, integrations, backend logging/error handling | .NET / Backend | `.github/skills/dotnet-role.md` | Use for `backend/api/` and `backend/etl-function/`. |
| Angular UI pages, components, services, dashboard interactions, frontend tests | Angular Frontend | `.github/skills/angular-frontend-role.md` | Use for anything under `frontend/`. |

## File-Path Based Routing

- Changes in `db/**` → Database skill first.
- Changes in `backend/api/**` or `backend/etl-function/**` → .NET / Backend skill first.
- Changes in `frontend/**` → Angular Frontend skill first.
- Changes in `promts/**` or requirements documents → Business Analyst skill first.

## Multi-Skill Routing Rules

For cross-layer tasks, combine skills in this order:

1. **Business Analyst**: confirm business intent and acceptance criteria.
2. **Database**: define data contracts/persistence artifacts.
3. **.NET / Backend**: implement APIs/functions and integration logic.
4. **Angular Frontend**: implement dashboard/UI consumption.

### Common Cross-Layer Examples

- **"Add dashboard job execution stats"**
  - Primary: Angular Frontend
  - Also: .NET / Backend (stats endpoint), Database (stats query/view)

- **"Show supplier change history in dashboard"**
  - Primary: Angular Frontend
  - Also: .NET / Backend (history endpoint), Database (snapshot/history query)

- **"Implement new ETL validation and persist results"**
  - Primary: .NET / Backend
  - Also: Database (validation log schema/SP)

## Conflict Resolution

If a task matches multiple skills equally:
- Use the skill associated with the **first changed code path** as primary owner.
- Add secondary skills for impacted layers.
- Keep acceptance criteria in sync with Business Analyst definitions.

## Output Expectation by Skill

- Business Analyst: Markdown requirements/stories with Given/When/Then.
- Database: SQL scripts/schema updates compatible with `db/EtlDatabase.sqlproj`.
- .NET / Backend: C# implementation with structured logging and configuration best practices.
- Angular Frontend: typed Angular components/services with clean UI states (loading/empty/error).
