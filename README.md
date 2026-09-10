# AutomationEngine

AutomationEngine is a .NET Windows service and web dashboard for managing scheduled automation jobs. It provides a local web UI for creating, running, enabling/disabling, and monitoring jobs, similar to Windows Task Scheduler but with a more observable and code-driven workflow.

The application runs as a hosted ASP.NET Core app with a Blazor server UI, uses SQLite for persistence, and exposes a REST API plus a SignalR status hub for real-time job updates.

## Highlights

- Schedule recurring jobs with cron expressions
- Support for process, PowerShell, and file cleanup job types
- Real-time job status updates through SignalR
- Persisted job definitions and execution history in SQLite
- Manual trigger, stop, enable/disable, and delete actions
- Windows service support for background execution
- Structured logging with Serilog and daily JSON/console log output
- Localhost-only access enforcement for safer local deployment

## Project status

This repository contains the application, its tests, and a WiX-based installer project for packaging a Windows deployment.

## Architecture

AutomationEngine is structured around a combination of hosted services, application-layer use cases, and a Blazor front end.

- ASP.NET Core 10 web app with Razor Components
- Blazor Server for the dashboard UI
- SignalR hub for live updates
- EF Core with SQLite for durable job storage
- Hosted background scheduler and queue services
- REST API under /api/jobs for job management
- Serilog for structured logging and log files under Logs/

## Main folders

- `AutomationEngine/` - application source
  - `Api/` - REST endpoints for jobs
  - `Application/` - job orchestration and state management
  - `Components/` - UI pages and Blazor components
  - `Composition/` - service registration and startup wiring
  - `Data/` - EF Core DbContext and entities
  - `Infrastructure/` - persistence, observability, and security components
  - `Services/` - scheduler, runner, queue, and notification services
  - `Options/` - typed app configuration
  - `Logs/` - generated runtime logs
- `AutomationEngine.Tests/` - automated tests
- `SetupAutomationEngine/` - WiX project for installation packaging

## Supported job types

### Process
Runs an executable or command with optional arguments and working directory.

### PowerShell
Executes a PowerShell script.

### FileCleanup
Deletes old files in a target folder based on age, recursion, and optional filename filter.

## Runtime behavior

The app initializes on startup by:

1. Validating database configuration
2. Applying EF Core migrations
3. Loading persisted jobs into memory
4. Cleaning up stale running states from previous failures

The scheduler then watches configured jobs and executes them according to each cron schedule.

## Configuration

The main runtime configuration is in `AutomationEngine/appsettings.json`.

Key settings include:

- `Database` - SQLite provider, timeout, pool, WAL settings
- `Server` - listening port for the web app
- `Security` - localhost-only enforcement
- `Resilience` - retry and circuit breaker settings
- `Scheduler` - background loop timing
- `Serilog` - console and file sinks

Default server URL is based on the configured port, which is typically:

- http://localhost:5001

## API overview

The application exposes a job API under `/api/jobs`.

### Common endpoints

- `GET /api/jobs` - list all jobs
- `GET /api/jobs/{jobId}` - fetch a job by ID
- `POST /api/jobs` - create or update a job
- `DELETE /api/jobs/{jobId}` - delete a job
- `PUT /api/jobs/{jobId}/enabled` - enable or disable a job
- `POST /api/jobs/{jobId}/trigger` - run a job immediately
- `POST /api/jobs/{jobId}/stop` - stop a running job
- `GET /api/jobs/{jobId}/exists` - check whether a job ID exists

The UI is backed by these endpoints and also uses SignalR for live status events on the job status hub.

## SignalR hub

The app exposes a job status hub for live updates when jobs start, finish, fail, or stop. The hub is mapped under the configured SignalR route used by the application.

## Logging

Structured logs are written to the `AutomationEngine/Logs` directory. The default configuration includes:

- daily console logs
- daily text logs
- daily JSON structured logs

This makes it easier to diagnose scheduler behavior, job timings, and runtime failures.

## Database

The app uses SQLite by default. Migrations are applied automatically during startup. The database file is configured in `appsettings.json` and is typically created in the working directory as `AutomationEngine.db`.

## Installer packaging

The `SetupAutomationEngine/` folder contains a WiX project for building an MSI installer. This is intended for creating a packaged Windows deployment.

## Development notes

- The service is configured to use Windows Service hosting when running as a service.
- The app enforces localhost access for request handling in the pipeline.
- Job states are validated and reloaded on startup to recover from stale process state.
- The codebase includes retry, circuit-breaker, and health-check patterns for resilience.

## Typical workflow

1. Start the app locally or install it as a Windows service.
2. Open the dashboard in the browser.
3. Create a job with a unique ID, type, schedule, and runtime configuration.
4. Save the job and enable it.
5. Monitor status and logs through the UI or API.
6. Trigger jobs manually when needed or let the scheduler execute them on the cron schedule.
