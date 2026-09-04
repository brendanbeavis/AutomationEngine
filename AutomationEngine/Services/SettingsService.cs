using AutomationEngine.Data;
using AutomationEngine.Data.Entities;
using AutomationEngine.Dto;
using AutomationEngine.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AutomationEngine.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IDbContextFactory<AutomationDbContext> _dbContextFactory;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(IDbContextFactory<AutomationDbContext> dbContextFactory, ILogger<SettingsService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Get current application settings
        /// </summary>
        public async Task<AppSettingsDto> GetSettingsAsync()
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var settings = await dbContext.AppSettings.FirstOrDefaultAsync(a => a.Id == 1);

                if (settings is null)
                {
                    _logger.LogWarning("AppSettings not found, creating default");
                    // Create default settings if not exists
                    settings = new AppSettingsEntity
                    {
                        Id = 1,
                        Theme = "cyberpunk",
                        LoggingLevel = "Information",
                        JobExecutionEnabled = true,
                        DefaultJobTimeoutSeconds = 300,
                        EnableDetailedLogging = false,
                        MaxJobHistoryRecords = 1000,
                        ApplicationName = "Automation Engine",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    dbContext.AppSettings.Add(settings);
                    await dbContext.SaveChangesAsync();
                }

                return MapToDto(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application settings");
                throw;
            }
        }

        /// <summary>
        /// Update application settings
        /// </summary>
        public async Task<AppSettingsDto> UpdateSettingsAsync(AppSettingsDto settingsDto)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var settings = await dbContext.AppSettings.FirstOrDefaultAsync(a => a.Id == 1);

                if (settings is null)
                {
                    settings = new AppSettingsEntity { Id = 1 };
                    dbContext.AppSettings.Add(settings);
                }

                // Update properties
                settings.Theme = settingsDto.Theme;
                settings.LoggingLevel = settingsDto.LoggingLevel;
                settings.JobExecutionEnabled = settingsDto.JobExecutionEnabled;
                settings.DefaultJobTimeoutSeconds = settingsDto.DefaultJobTimeoutSeconds;
                settings.NtfyEndpoint = settingsDto.NtfyEndpoint;
                settings.EnableDetailedLogging = settingsDto.EnableDetailedLogging;
                settings.MaxJobHistoryRecords = settingsDto.MaxJobHistoryRecords;
                settings.ApplicationName = settingsDto.ApplicationName;
                settings.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Application settings updated");

                return MapToDto(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application settings");
                throw;
            }
        }

        /// <summary>
        /// Get current theme
        /// </summary>
        public async Task<string> GetThemeAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.Theme;
        }

        /// <summary>
        /// Update theme
        /// </summary>
        public async Task<string> UpdateThemeAsync(string theme)
        {
            var settings = await GetSettingsAsync();
            settings.Theme = theme;
            var updated = await UpdateSettingsAsync(settings);
            return updated.Theme;
        }

        /// <summary>
        /// Map entity to DTO
        /// </summary>
        private AppSettingsDto MapToDto(AppSettingsEntity entity)
        {
            return new AppSettingsDto
            {
                Id = entity.Id,
                Theme = entity.Theme,
                LoggingLevel = entity.LoggingLevel,
                JobExecutionEnabled = entity.JobExecutionEnabled,
                DefaultJobTimeoutSeconds = entity.DefaultJobTimeoutSeconds,
                NtfyEndpoint = entity.NtfyEndpoint,
                EnableDetailedLogging = entity.EnableDetailedLogging,
                MaxJobHistoryRecords = entity.MaxJobHistoryRecords,
                ApplicationName = entity.ApplicationName,
                UpdatedAt = entity.UpdatedAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
