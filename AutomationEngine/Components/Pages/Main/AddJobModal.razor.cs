using AutomationEngine.Configuration;
using AutomationEngine.Dto;
using AutomationEngine.Models;
using AutomationEngine.Models.Cron;
using AutomationEngine.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class AddJobModal
    {
        [Parameter]
        public bool IsVisible { get; set; } = false;

        [Parameter]
        public JobDto Job { get; set; } = new JobDto();

        [Parameter]
        public bool IsEdit { get; set; } = false;
        [Parameter]
        public bool IsDupe { get; set; } = false;

        [Parameter]
        public string ErrorMessage { get; set; } = string.Empty;

        [Parameter]
        public EventCallback<JobDto> OnSave { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public CronSchedule Schedule { get; set; } = new();

        [Inject]
        private NavigationManager? NavManager { get; set; }

        [Inject]
        private JobApiClient? JobApi { get; set; }

        [Inject]
        private ILogger<AddJobModal> _logger { get; set; } = default!;

        private EditContext? EditContext;
        private string oldCommand = string.Empty;
        private CronSchedule ScheduleValue { get; set; } = new();

        protected override void OnInitialized()
        {
            EditContext = new EditContext(Job);
            ScheduleValue = Schedule;
        }

        protected override void OnParametersSet()
        {
            if (EditContext == null || EditContext.Model != Job)
            {
                EditContext = new EditContext(Job);
            }
        }

        private string FieldClass(string fieldName, bool isCheckbox = false)
        {
            if (EditContext == null || Job == null)
                return isCheckbox ? "form-check-input" : "form-control";

            var prop = Job.GetType().GetProperty(fieldName);
            var isSelect = prop != null && prop.PropertyType.IsEnum;
            var fi = new FieldIdentifier(Job, fieldName);
            var hasMessages = EditContext.GetValidationMessages(fi).Any();

            if (isCheckbox)
                return hasMessages ? "form-check-input is-invalid" : "form-check-input";
            if (isSelect)
                return hasMessages ? "form-select is-invalid" : "form-select";
            return hasMessages ? "form-control is-invalid" : "form-control";
        }

        private void OnJobTypeChanged()
        {
            if (Job.Type == JobType.PowerShell)
            {
                oldCommand = Job.Command;
                Job.Command = Constants.Jobs.PowerShellDefault;
            }
            else if (Job.Type == JobType.FileCleanup)
            {
                oldCommand = Job.Command;
                Job.Command = string.Empty;
            }
            else if (Job.Type == JobType.Process)
            {
                Job.Command = oldCommand;
            }
        }

        private void OnScheduleChanged(CronSchedule value)
        {
            ScheduleValue = value;
            try
            {
                var cron = CronExpressionGenerator.Generate(value);
                _logger.LogDebug("Cron expression generated: {CronExpression}", cron);
                Job.Schedule = cron;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to generate cron expression from schedule");
            }
        }

        private async Task OnSubmitClick()
        {
            // Validate Job ID is unique (only when creating new jobs)
            if (!IsEdit && !string.IsNullOrEmpty(Job.JobId) && JobApi != null)
            {
                try
                {
                    var exists = await JobApi.JobIdExistsAsync(Job.JobId);
                    if (exists)
                    {
                        ErrorMessage = $"A job with ID '{Job.JobId}' already exists. Please use a different ID.";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check Job ID uniqueness | JobId: {JobId}", Job.JobId);
                    ErrorMessage = $"Error checking Job ID: {ex.Message}";
                    return;
                }
            }

            // Clear error message before saving
            ErrorMessage = string.Empty;
            await OnSave.InvokeAsync(Job);
        }

        private async Task OnCloseClick()
        {
            await OnClose.InvokeAsync();
        }
    }
}
