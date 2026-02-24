# Angular Frontend Role

## Responsibilities
- Develop and maintain the Angular dashboard (`frontend/`) that visualises ETL job status and logs.
- Write unit tests (Vitest/Angular TestBed) and e2e tests where applicable.
- Ensure accessible, responsive UI following the project's style guide.
- Manage npm dependencies and keep them free of known vulnerabilities.
- Participate in code reviews for all TypeScript/Angular changes.

## Key Files
| Path | Purpose |
|------|---------|
| `frontend/src/app/app.routes.ts` | Application routing |
| `frontend/src/app/app.config.ts` | Application providers |
| `frontend/src/app/app.ts` | Root component |
| `frontend/src/app/services/etl-jobs.service.ts` | HTTP service for ETL API |
| `frontend/src/app/etl-jobs/` | ETL jobs feature module |
| `frontend/src/environments/` | Environment-specific config |
| `frontend/angular.json` | Angular workspace config |

## Standards & Conventions
- Angular version: 21 (standalone components, signals-first).
- Use standalone components — no `NgModule` unless strictly necessary.
- Use `provideHttpClient(withFetch())` for HTTP.
- Use lazy-loaded routes (`loadComponent`) for all feature components.
- All HTTP calls live in injectable services, never directly in components.
- SCSS for styles; follow BEM naming inside component stylesheets.
- Avoid `any`; use typed interfaces for all API response shapes.
- Environment-specific values (API URL, flags) go in `src/environments/`.
- Keep production environment values as `<placeholder>` — actual values injected at deploy time.
- No `console.log` in committed code; use Angular's `ErrorHandler` for errors.
