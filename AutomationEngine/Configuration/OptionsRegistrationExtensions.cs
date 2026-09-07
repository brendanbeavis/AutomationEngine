using AutomationEngine.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutomationEngine.Configuration
{
    public static class OptionsRegistrationExtensions
    {
        public static IServiceCollection AddAutomationEngineOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .Validate(options => options.IsValid(out _), "Database configuration is invalid")
                .ValidateOnStart();

            services
                .AddOptions<ServerOptions>()
                .Bind(configuration.GetSection(ServerOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddOptions<SecurityOptions>()
                .Bind(configuration.GetSection(SecurityOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddOptions<NtfyOptions>()
                .Bind(configuration.GetSection(NtfyOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddOptions<ResilienceOptions>()
                .Bind(configuration.GetSection(ResilienceOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddOptions<SchedulerOptions>()
                .Bind(configuration.GetSection(SchedulerOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddOptions<JobApiOptions>()
                .Bind(configuration.GetSection(JobApiOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddOptions<DesignTimeDatabaseOptions>()
                .Bind(configuration.GetSection(DesignTimeDatabaseOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
