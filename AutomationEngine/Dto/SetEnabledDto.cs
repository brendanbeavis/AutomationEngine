using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Dto
{
    /// <summary>
    /// Data Transfer Object for updating the enabled status of a job.
    /// </summary>
    /// <remarks>
    /// Simple DTO used for API requests to toggle the enabled/disabled state of a job.
    /// This separate DTO ensures focused endpoint contracts and clear request intent.
    /// </remarks>
    public class SetEnabledDto
    {
        /// <summary>
        /// Indicates whether the job should be enabled or disabled.
        /// </summary>
        [Required(ErrorMessage = "Enabled status is required")]
        public bool Enabled { get; set; }
    }
}

