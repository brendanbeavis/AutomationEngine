using System.Linq;

namespace AutomationEngine.Components.Pages.Main
{
    public partial class Index
    {
        private void CalculateJobSummary()
        {
            if (jobs is null || jobs.Count == 0)
            {
                TotalJobs = 0;
                TotalEnabled = 0;
                TotalDisabled = 0;
                TotalFailed = 0;
                TotalSucceeded = 0;
                TotalRunning = 0;
                TotalQueued = 0;
                return;
            }

            TotalJobs = jobs.Count;
            TotalEnabled = jobs.Count(j => j.Enabled);
            TotalDisabled = jobs.Count(j => !j.Enabled);
            TotalFailed = jobs.Count(j => j.LastRunSuccess == false);
            TotalSucceeded = jobs.Count(j => j.LastRunSuccess == true);
            TotalRunning = jobs.Count(j => j.IsRunning);
            TotalQueued = jobs.Count(j => j.Enabled && !j.IsRunning);
        }
    }
}
