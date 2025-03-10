using System;

namespace SqlMonitor.Models
{
    public class IndexInfo
    {
        public string DatabaseName { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string IndexName { get; set; }
        public double FragmentationPercentage { get; set; }
        public long PageCount { get; set; }
        public DateTime LastReindexed { get; set; }
        public bool NeedsReindexing => FragmentationPercentage > 30;
        public string ReindexType => FragmentationPercentage > 30 ? "REBUILD" : (FragmentationPercentage > 10 ? "REORGANIZE" : "None");
    }
} 