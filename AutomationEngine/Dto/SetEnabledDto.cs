using System.ComponentModel.DataAnnotations;

namespace AutomationEngine.Dto
{
    public class SetEnabledDto
    {
        [Required(ErrorMessage = "Enabled status is required")]
        public bool Enabled { get; set; }
    }
}

