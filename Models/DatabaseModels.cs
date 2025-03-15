using System;
using System.Collections.Generic;

namespace SqlMonitor.Models
{
    public class DatabaseInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Server { get; set; }
        public string? Status { get; set; }
    }
    
    public class PerformanceMetrics
    {
        public List<string> Timestamps { get; set; } = new List<string>();
        public List<double> Cpu { get; set; } = new List<double>();
        public List<double> Memory { get; set; } = new List<double>();
        public List<double> DiskIO { get; set; } = new List<double>();
        public List<double> NetworkIO { get; set; } = new List<double>();
    }
    
    // SlowQuery class is now defined in its own file: Models/SlowQuery.cs
    
    public class MissingIndex
    {
        public string? Id { get; set; }
        public string? Table { get; set; }
        public string? Columns { get; set; }
        public string? IncludeColumns { get; set; }
        public string? EstimatedImpact { get; set; }
        public int ImprovementPercent { get; set; }
        public string? CreateStatement { get; set; }
        public bool Created { get; set; }
    }
    
    public class SlowQuerySimulationResult
    {
        public string? Message { get; set; }
        public double ExecutionTime { get; set; }
        public string? QueryId { get; set; }
        public string? QueryText { get; set; }
    }
    
    public class IndexCreationResult
    {
        public string? Message { get; set; }
        public string? IndexName { get; set; }
        public string? PerformanceImprovement { get; set; }
    }
    
    public class QueryFixResult
    {
        public string? Message { get; set; }
        public string? OriginalQuery { get; set; }
        public string? OptimizedQuery { get; set; }
        public string? Explanation { get; set; }
        public List<string> IndexRecommendations { get; set; } = new List<string>();
        public PerformanceComparison? PerformanceBefore { get; set; }
        public PerformanceComparison? PerformanceAfter { get; set; }
        public string? ImprovementPercent { get; set; }
        public bool AIPowered { get; set; }
        public bool OptimizedQueryWorks { get; set; }
    }
    
    public class PerformanceComparison
    {
        public double ExecutionTime { get; set; }
        public double CpuTime { get; set; }
        public int LogicalReads { get; set; }
    }
    
    public class QueryOptimizationResult
    {
        public string? OptimizedQuery { get; set; }
        public string? Explanation { get; set; }
        public List<string> IndexRecommendations { get; set; } = new List<string>();
        public bool IsSimulated { get; set; }
    }
} 