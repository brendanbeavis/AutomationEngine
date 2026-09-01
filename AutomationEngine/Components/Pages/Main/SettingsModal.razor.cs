using AutomationEngine.Dto;
using AutomationEngine.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class SettingsModal
    {
        [Inject]
        private SettingsService SettingsService { get; set; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        [Inject]
        private ILogger<SettingsModal> _logger { get; set; } = default!;

        [Parameter]
        public bool IsVisible { get; set; } = false;

        [Parameter]
        public EventCallback OnClose { get; set; }

        // Settings data
        private AppSettingsDto? Settings { get; set; }

        // UI state
        private string? ErrorMessage { get; set; }
        private string? SuccessMessage { get; set; }
        private int ActiveTab { get; set; } = 0; // 0 = General, 1 = Theme, 2 = Database
        private bool IsSaving { get; set; } = false;
        private string? SelectedTheme { get; set; }

        // Theme options
        private readonly List<(string Name, string Value, string Description)> Themes = new()
        {
            ("Cyberpunk", "cyberpunk", "Neon colors with glitch effects - futuristic and bold"),
            ("Dark Mode", "dark", "Sleek dark interface with subtle accents"),
            ("Light Mode", "light", "Clean light interface for better readability")
        };

        protected override async Task OnParametersSetAsync()
        {
            if (IsVisible && Settings == null)
            {
                await LoadSettingsAsync();
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                ErrorMessage = null;
                Settings = await SettingsService.GetSettingsAsync();
                SelectedTheme = Settings.Theme;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load settings from service");
                ErrorMessage = $"Failed to load settings: {ex.Message}";
                Settings = new AppSettingsDto(); // Return safe default
            }
        }

        private async Task SaveSettingsAsync()
        {
            if (Settings == null)
                return;

            try
            {
                IsSaving = true;
                ErrorMessage = null;

                var updated = await SettingsService.UpdateSettingsAsync(Settings);
                Settings = updated;
                SelectedTheme = updated.Theme;

                SuccessMessage = "Settings saved successfully!";

                // Apply theme immediately
                if (SelectedTheme != null)
                {
                    await JS.InvokeVoidAsync("window.applyTheme", SelectedTheme);
                }

                // Clear success message after 3 seconds
                await Task.Delay(3000);
                SuccessMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                ErrorMessage = $"Failed to save settings: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task SelectThemeAsync(string theme)
        {
            if (Settings == null)
                return;

            SelectedTheme = theme;
            Settings.Theme = theme;

            // Preview the theme immediately
            await JS.InvokeVoidAsync("window.applyTheme", theme);

            // Auto-save
            await SaveSettingsAsync();
        }

        private async Task OnCloseClick()
        {
            Settings = null;
            ErrorMessage = null;
            SuccessMessage = null;
            ActiveTab = 0;
            await OnClose.InvokeAsync();
        }

        private void SetActiveTab(int tab)
        {
            ActiveTab = tab;
        }

        // Validation helpers
        private string GetValidationClass(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "is-invalid";
            return "is-valid";
        }
    }
}
