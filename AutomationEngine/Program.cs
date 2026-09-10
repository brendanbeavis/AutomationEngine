using AutomationEngine.Composition;
using Serilog;
using System.Reflection;

var exeDirectory = AppContext.BaseDirectory;

try
{
    Directory.SetCurrentDirectory(exeDirectory);
    Directory.CreateDirectory(Path.Combine(exeDirectory, "Logs"));
}
catch
{
    // Fallback to default working directory if setting the service directory fails.
}

// Configure Serilog with structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(exeDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AutomationEngine")
    .CreateLogger();

Log.Debug("Application working directory set to {WorkingDirectory}", Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);
Log.Information("WebApplication builder created");

builder.Host
    .UseWindowsService()
    .UseSerilog();

Log.Information("Host configuration: WindowsService enabled, Serilog logging configured");
Log.Information("Application version: {Version} | Framework: .NET 10 | Environment: {Environment}",
    Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
    builder.Environment.EnvironmentName);

builder.ConfigureAutomationEngineServices();

var app = builder.Build();
await app.InitializeAutomationEngineAsync();
app.UseAutomationEnginePipeline();

await app.RunAsync();
