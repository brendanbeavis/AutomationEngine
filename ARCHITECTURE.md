# AutomationEngine Architecture Map

## Layer-First Structure

- `AutomationEngine/Presentation`
  - `Api`
  - `Blazor`
  - `SignalR`
- `AutomationEngine/Application`
  - `Abstractions`
  - `UseCases`
  - `Contracts`
  - `Validation`
- `AutomationEngine/Domain`
  - `Entities`
  - `Enums`
- `AutomationEngine/Infrastructure`
  - `Persistence`
  - `Execution`
  - `Messaging`
  - `Notifications`
  - `Configuration`
  - `Observability`
  - `Security`
- `AutomationEngine/Composition`
  - `ServiceRegistration`
  - `StartupTasks`
  - `EndpointMapping`

## Dependency Rules

1. `Presentation` depends on `Application` only.
2. `Application` depends on `Domain` and `Application.Abstractions` only.
3. `Infrastructure` implements `Application.Abstractions` and depends on `Application` + `Domain`.
4. `Domain` has no dependencies on `Presentation` or `Infrastructure`.
5. `Program.cs` is a thin composition root delegating to `Composition/*` modules.

## Transitional Constraints

- Keep current runtime behavior.
- Keep API DTOs separate from application contracts.
- Align validation rules between API DTOs and application contracts via shared constants.
- Use interfaces in controllers/hosted services, avoid concrete service coupling.
