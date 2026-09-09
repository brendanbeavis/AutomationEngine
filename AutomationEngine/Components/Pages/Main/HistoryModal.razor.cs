using AutomationEngine.Dto;
using Microsoft.AspNetCore.Components;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class HistoryModal
    {
        [Parameter]
        public bool IsVisible { get; set; } = false;

        [Parameter]
        public string JobId { get; set; } = string.Empty;

        [Parameter]
        public List<JobRunDto>? Runs { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        private async Task OnCloseClick()
        {
            await OnClose.InvokeAsync();
        }

        // Dashboard Metrics Properties
        public decimal SuccessRate => CalculateSuccessRate();
        public double AverageDuration => CalculateAverageDuration();
        public int TotalRuns => Runs?.Count ?? 0;
        public DateTime? LastRunTime => GetLastRunTime();
        public bool LastRunSuccessful => GetLastRunSuccessful();
        public string LastRunStatus => GetLastRunStatus();

        private decimal CalculateSuccessRate()
        {
            if (Runs == null || Runs.Count == 0)
                return 0m;

            int successCount = Runs.Count(r => r.Success);
            return Math.Round((decimal)successCount / Runs.Count * 100, 1);
        }

        private double CalculateAverageDuration()
        {
            if (Runs == null || Runs.Count == 0)
                return 0;

            var runsWithDuration = Runs.Where(r => r.DurationMs.HasValue).ToList();
            if (runsWithDuration.Count == 0)
                return 0;

            return Math.Round(runsWithDuration.Average(r => r.DurationMs.Value), 0);
        }

        private DateTime? GetLastRunTime()
        {
            return Runs?.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.StartedAt;
        }

        private bool GetLastRunSuccessful()
        {
            return Runs?.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.Success ?? false;
        }

        private string GetLastRunStatus()
        {
            if (Runs == null || Runs.Count == 0)
                return "No runs";

            var lastRun = Runs.OrderByDescending(r => r.StartedAt).FirstOrDefault();
            if (lastRun == null)
                return "No runs";

            if (lastRun.Success)
                return "Succeeded";
            else
                return "Failed";
        }

        public string GetStatusBadgeClass(bool success)
        {
            return success ? "badge-success" : "badge-danger";
        }

        public string GetStatusTextClass(bool success)
        {
            return success ? "text-success" : "text-danger";
        }
    }
}
