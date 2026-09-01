using AutomationEngine.Dto;

namespace AutomationEngine.Services.Abstractions
{
    /// <summary>
    /// Interface for application settings management service
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Get current application settings
        /// </summary>
        Task<AppSettingsDto> GetSettingsAsync();

        /// <summary>
        /// Update application settings
        /// </summary>
        Task<AppSettingsDto> UpdateSettingsAsync(AppSettingsDto settingsDto);

        /// <summary>
        /// Get current theme
        /// </summary>
        Task<string> GetThemeAsync();

        /// <summary>
        /// Update theme
        /// </summary>
        Task<string> UpdateThemeAsync(string theme);
    }
}
