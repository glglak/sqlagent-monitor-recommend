using System;

namespace SqlMonitor.Models
{
    public class SlowQueryHistory
    {
        public int Id { get; set; }
        public string QueryText { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public double AverageDurationMs { get; set; }
        public int ExecutionCount { get; set; }
        public DateTimeOffset FirstSeen { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public string QueryPlan { get; set; } = string.Empty;
        public string OptimizationSuggestions { get; set; } = string.Empty;
        public SlowQuerySeverity Severity { get; set; }
        public bool IsResolved { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public DateTimeOffset? ResolvedAt { get; set; }
    }

    public enum SlowQuerySeverity
    {
        Normal,
        Warning,
        Critical
    }
} 