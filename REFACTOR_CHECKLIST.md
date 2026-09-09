# Refactor Checklist (Layer-First)

## Completed in this pass

- [x] Added architecture map and dependency rules (`ARCHITECTURE.md`)
- [x] Added repo standards files:
  - [x] `.editorconfig`
  - [x] `Directory.Build.props`
  - [x] `Directory.Packages.props`
  - [x] `global.json`
- [x] Modernized package references and removed legacy ASP.NET packages from main project
- [x] Split composition concerns from `Program.cs` into:
  - [x] `AutomationEngine/Composition/ServiceRegistrationExtensions.cs`
  - [x] `AutomationEngine/Composition/StartupInitializationExtensions.cs`
  - [x] `AutomationEngine/Composition/EndpointMappingExtensions.cs`
- [x] Enforced interface-first wiring for key runtime services:
  - [x] `JobsController` now consumes `IJobStateManager` and `IJobRunner`
  - [x] `SchedulerService` now consumes `IJobStateManager`
  - [x] DI registration maps interfaces to implementations
- [x] Moved infrastructure implementations to layer folders:
  - [x] `Infrastructure/Persistence/AutomationDbContext.cs`
  - [x] `Infrastructure/Persistence/DesignTimeDbContextFactory.cs`
  - [x] `Infrastructure/Persistence/Repositories/JobRepository.cs`
  - [x] `Infrastructure/Security/Middleware/CorrelationIdMiddleware.cs`
  - [x] `Infrastructure/Security/Middleware/LocalhostOnlyMiddleware.cs`
  - [x] `Infrastructure/Observability/StructuredLogger.cs`
  - [x] `Infrastructure/Observability/LoggingContext.cs`
  - [x] `Infrastructure/Observability/ErrorClassification.cs`
- [x] Moved orchestration artifacts:
  - [x] `Application/UseCases/Jobs/JobStateManager.cs`
  - [x] `Application/Abstractions/IJobStateManager.cs`
- [x] Centralized validation rules in `Application/Validation/JobValidationRules.cs`
- [x] Aligned validation contracts in `JobDto` and `SaveJobRequest`
- [x] Fixed persistence semantics:
  - [x] `DeleteJobAsync` now soft-deletes
  - [x] Added `HardDeleteJobAsync` in repository contract/implementation
- [x] Blazor presentation refactor:
  - [x] `DatabaseViewer.razor.cs` uses `IDbContextFactory<AutomationDbContext>`
  - [x] `Index.razor.cs` summary logic split into `Index.Summary.cs`
- [x] Added architecture tests:
  - [x] `AutomationEngine.Tests/Architecture/LayeringRulesTests.cs`

## Validation completed

- [x] Solution build passed (`run_build`)
- [x] Targeted architecture tests passed (`AutomationEngine.Tests.Architecture`)

## Follow-up items (next PR)

- [x] Complete namespace normalization to align with new folder layout (currently transitional in some files)
  - [x] Consolidated all contracts from `Services/Abstractions` into `Application/Abstractions`
  - [x] Normalized abstraction namespaces to `AutomationEngine.Application.Abstractions`
  - [x] Updated app/test abstraction references and removed stale `Services/Abstractions` path
- [ ] Address remaining existing test-suite behavioral failures in integration/unit tests
- [ ] Add stronger architecture tests (forbidden references between layers)
- [ ] Consider splitting into multi-project solution (`Domain`, `Application`, `Infrastructure`, `Presentation`) once boundaries stabilize
- [X] Remove explicit `Microsoft.Extensions.Hosting` package reference (NU1510 warning)
- [X] Resolve vulnerable transitive package warning (`SQLitePCLRaw.lib.e_sqlite3`)
