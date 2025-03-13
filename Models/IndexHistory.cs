using System;

namespace SqlMonitor.Models
{
    public class IndexHistory
    {
        public int Id { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public double FragmentationPercentage { get; set; }
        public long PageCount { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public DateTime OperationDate { get; set; }
        public int? OperationDurationMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
} 