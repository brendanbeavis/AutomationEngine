namespace AutomationEngine.Dto
{
    public class JobRunDto
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string StdOut { get; set; } = "";
        public string StdErr { get; set; } = "";
        public int? DurationMs { get; set; }
    }
}
