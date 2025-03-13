using System;

namespace SqlMonitor.Models
{
    public class SlowQuery
    {
        public string QueryText { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public double AverageDurationMs { get; set; }
        public int ExecutionCount { get; set; }
        public DateTimeOffset LastExecutionTime { get; set; }
        public string QueryPlan { get; set; } = string.Empty;
        public string OptimizationSuggestions { get; set; } = string.Empty;
    }
} 