# AutomationEngine
AutomationEngine is a windows service for scheduling and triggering regular processes. Similar to native windows scheduled tasks.




#TODO

more theme options
fix light theme



saving a job that matches a id of a previously deleted job causes an errpr "Error saving job: BadRequest"

What is Running JobsPanel?
review models.cron structure

ntfy payload:
{"title":"Job failed: TestPS1 (Copy)","message":"Timed out"}
when a scheduled backgroupnd job is running, why isnt dashboard updated to show as running



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