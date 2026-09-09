using AutomationEngine.Composition;
using Serilog;
using System.Reflection;

// Configure Serilog with structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AutomationEngine")
    .CreateLogger();

try
{
    var exePath = Assembly.GetExecutingAssembly().Location;
    var exeDirectory = Path.GetDirectoryName(exePath);
    Directory.SetCurrentDirectory(exeDirectory);
    Log.Debug("Application working directory set to {WorkingDirectory}", exeDirectory);
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to set working directory, continuing with default");
}

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
