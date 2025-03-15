using System;

namespace SqlMonitor.Models
{
    public class SlowQuery
    {
        // Primary identifier
        public string? Id { get; set; }
        
        // Query details
        public string? Query { get; set; }
        public double ExecutionTime { get; set; }
        public double CpuTime { get; set; }
        public int LogicalReads { get; set; }
        public int ExecutionCount { get; set; }
        public string? SuggestedFix { get; set; }
        public bool Fixed { get; set; }
        
        // Alternative properties (used in different contexts)
        public string? QueryText { get; set; }
        public string? DatabaseName { get; set; }
        public double AverageDurationMs { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public string? QueryPlan { get; set; }
        public string? OptimizationSuggestions { get; set; }
    }
} 