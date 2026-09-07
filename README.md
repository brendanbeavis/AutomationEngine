# AutomationEngine
AutomationEngine is a windows service for scheduling and triggering regular processes. Similar to native windows scheduled tasks.

## Configuration (typed options)

Configuration is consolidated into typed options bound from `AutomationEngine/appsettings*.json`.

### Sections

- `Database` -> `DatabaseOptions`
- `Server` -> `ServerOptions`
- `Security` -> `SecurityOptions`
- `Ntfy` -> `NtfyOptions`
- `Resilience` -> `ResilienceOptions`
- `Scheduler` -> `SchedulerOptions`
- `JobApi` -> `JobApiOptions`
- `DesignTimeDatabase` -> `DesignTimeDatabaseOptions` (EF tooling overrides)

### Validation behavior

- Options are bound centrally in startup using `AddAutomationEngineOptions(...)`.
- Validation is strict (`ValidateOnStart`) and startup fails fast for invalid/missing values.

### Environment variable binding examples

Use `__` for nested keys:

- `Database__FilePath`
- `Database__CommandTimeoutSeconds`
- `Server__Port`
- `Security__LocalhostOnly`
- `Resilience__RetryAttempts`
- `Scheduler__ReloadIntervalSeconds`
- `JobApi__HistoryLimit`
- `DesignTimeDatabase__FilePath`

### EF Core design-time notes

- `DesignTimeDbContextFactory` now loads `appsettings.json`, optional `appsettings.{ENV}.json`, and environment variables.
- It binds `Database` and applies optional `DesignTimeDatabase` overrides.
- `DesignTimeDatabase` is intended for migration/tooling-specific values (for example `db\\AutomationEngine.db`) without changing runtime `Database` settings.







#TODO

more theme options
fix light theme, remove glow from text, table has dark background



What is Running JobsPanel?


theres an exception in ntfy service.







•	Adding unit tests for the extension methods
•	Creating additional domain-specific extensions if new features require it
•	Adding logging/telemetry to the startup pipeline
•	Documenting any configuration requirements for deployment scenarios

1.	Configuration Consolidation – Consolidate scattered app settings into a typed configuration options pattern
2.	Logging Improvements – Enhance structured logging and diagnostic coverage
3.	Test Coverage Expansion – Increase unit and integration test coverage
4.	Performance Optimization – Profiling and optimization opportunities



AutomationEngine/
├── src/                    # All code
│   ├── Api/
│   │   ├── Controllers/
│   │   └── Requests/      # API request models
│   ├── Components/
│   ├── Data/
│   ├── Features/           # Feature-organized folders (alternative)
│   ├── Infrastructure/     # Cross-cutting concerns
│   │   ├── Middleware/
│   │   ├── Logging/
│   │   └── Resilience/
│   ├── Models/
│   ├── Services/
│   │   ├── Abstractions/
│   │   ├── Domain/        # Domain services
│   │   └── Infrastructure/ # Infrastructure services
│   └── Shared/             # Shared models, constants, extensions
├── tests/                  # Test projects
└── config/                 # Configuration files