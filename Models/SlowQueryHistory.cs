namespace SqlMonitor.Models
{
    public class SlowQueryHistory
    {
        public int Id { get; set; }
        public string QueryText { get; set; }
        public string DatabaseName { get; set; }
        public double AverageDurationMs { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public string QueryPlan { get; set; }
        public string OptimizationSuggestions { get; set; }
        public SlowQuerySeverity Severity { get; set; }
        public bool IsResolved { get; set; }
        public string Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public enum SlowQuerySeverity
    {
        Normal,
        Warning,
        Critical
    }
} 