using AutomationEngine.Configuration;
using AutomationEngine.Infrastructure.Security.Middleware;
using AutomationEngine.Options.Interfaces;
using AutomationEngine.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;

namespace AutomationEngine.Composition;

public static class EndpointMappingExtensions
{
    public static void UseAutomationEnginePipeline(this WebApplication app)
    {
        app.UseCorrelationId();
        app.UseMiddleware<LocalhostOnlyMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.MapControllers();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAntiforgery();

        app.MapRazorComponents<AutomationEngine.Components.App>()
            .AddInteractiveServerRenderMode();
        app.MapHub<JobStatusHub>(Constants.SignalR.JobStatusHubPath);

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var serverOptions = app.Services.GetRequiredService<IServerOptions>();

            Log.Information("APPLICATION LIFECYCLE | Event: ApplicationStarted | Version: {Version} | Environment: {Environment} | Url: {Url}",
                version, app.Environment.EnvironmentName, serverOptions.LocalhostUrl);
        });

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            Log.Information("APPLICATION LIFECYCLE | Event: ApplicationStopping");
        });

        app.Lifetime.ApplicationStopped.Register(() =>
        {
            Log.Information("APPLICATION LIFECYCLE | Event: ApplicationStopped");
            Log.CloseAndFlush();
        });
    }
}
