using System;

namespace SqlMonitor.Models
{
    public class IndexInfo
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public double FragmentationPercentage { get; set; }
        public double FragmentationPercent { get; set; } // Alias for compatibility
        public long PageCount { get; set; }
        public DateTime? LastReindexed { get; set; }
        public bool NeedsReindexing => FragmentationPercentage > 30;
        public string ReindexType { get; set; } = "REORGANIZE"; // REORGANIZE or REBUILD
    }
} 