namespace SqlMonitor.Models
{
    public class SqlServerSettings
    {
        public string ConnectionString { get; set; }
        public int MonitoringIntervalMinutes { get; set; }
        public int SlowQueryThresholdMs { get; set; }
        public int IndexFragmentationThreshold { get; set; }
    }
} 