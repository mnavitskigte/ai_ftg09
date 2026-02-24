# EtlDashboard

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.1.5.

## Run with API (Recommended)

From the workspace root in VS Code, use the predefined tasks in `.vscode/tasks.json`:

- `Run Frontend`
- `Run API`
- `Run API + Frontend` (starts both in parallel)

### Start
1. Open Command Palette (`Ctrl+Shift+P`)
2. Run `Tasks: Run Task`
3. Select `Run API + Frontend`

### URLs
- Frontend: `http://localhost:4200`
- API: `http://localhost:5172`
- Swagger: `http://localhost:5172/swagger`

### Stop
- Use `Tasks: Terminate Task` for `Run Frontend` / `Run API`, or press `Ctrl+C` in each task terminal.

### Dashboard data mode
- The dashboard defaults to **Mock** mode on load.
- Use the **Data Source** selector in the header to switch between `Mock` and `Real (DB)`.

## Troubleshooting

- **API port conflict (`5172` already in use)**
	```powershell
	Get-NetTCPConnection -LocalPort 5172 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }
	```

- **Frontend loads but API calls fail**
	- Verify API is running: `http://localhost:5172/swagger`.
	- Verify app points to `http://localhost:5172` in `src/environments/environment.ts`.
	- Verify API key values match between frontend and backend config.

- **Mock mode selected but little/no data appears**
	- Check header Data Source is `Mock`.
	- Use the card header "Mock source" label to confirm if data is from API or local fallback.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
