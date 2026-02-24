# Business Analyst Role

## Responsibilities
- Define, document and prioritise functional requirements for the ETL platform.
- Write clear user stories and acceptance criteria in GitHub Issues.
- Maintain the business glossary and data dictionary.
- Liaise with stakeholders to gather requirements and validate delivered features.
- Review and sign off on API contracts and UI wireframes before development starts.
- Track KPIs and SLAs for ETL job reliability (success rate, processing time, data freshness).

## Domain Concepts

| Term | Definition |
|------|-----------|
| **ETL Job** | A named data pipeline that extracts, transforms and loads data between systems. |
| **ETL Job Log** | An execution record for a single run of an ETL Job (start time, end time, rows processed, status). |
| **Cron Schedule** | A six-field CRON expression (Azure Functions format) that determines when a job fires automatically. |
| **Run-ETL Function** | The Azure Function that fires on schedule and orchestrates all enabled ETL jobs. |
| **ETL API** | The .NET 8 Web API that exposes job metadata and execution history to the frontend. |
| **ETL Dashboard** | The Angular web application used by operators to monitor job status and review logs. |

## Key Metrics / KPIs
- **Job Success Rate**: percentage of completed job runs with `Status = 'Completed'` over a rolling 7-day window. Target ≥ 99%.
- **Average Processing Time**: mean `DurationSeconds` per job. Alert if > 2× baseline.
- **Data Freshness**: time elapsed since the last successful run of each enabled job. Alert if > scheduled interval × 1.5.

## User Stories (seed backlog)

```
As an operator, I want to see all ETL jobs and their last run status on the dashboard
  so that I can quickly identify failing pipelines.

As an operator, I want to view the execution log for a specific job
  so that I can investigate errors without accessing the database directly.

As a developer, I want the ETL function to log every run start and completion to the database
  so that we have a full audit trail.

As a data engineer, I want to enable/disable individual ETL jobs without code changes
  so that I can pause pipelines during maintenance windows.
```

## Acceptance Criteria Template
```
Given [precondition]
When  [action]
Then  [expected outcome]
And   [additional assertions]
```
