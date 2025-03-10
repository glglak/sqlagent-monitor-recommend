using System;

namespace SqlMonitor.Models
{
    public class SlowQuery
    {
        public string QueryText { get; set; }
        public string DatabaseName { get; set; }
        public double AverageDurationMs { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public string QueryPlan { get; set; }
        public string OptimizationSuggestions { get; set; }
    }
} 