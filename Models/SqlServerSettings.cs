using System.Collections.Generic;

namespace SqlMonitor.Models
{
    public class SqlServerSettings
    {
        public string? ConnectionString { get; set; }
        public int CommandTimeout { get; set; } = 30;
        public bool UseWindowsAuth { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int MonitoringIntervalMinutes { get; set; }
        public int SlowQueryThresholdMs { get; set; }
        public int IndexFragmentationThreshold { get; set; }
        public List<MonitoredDatabase> MonitoredDatabases { get; set; } = new List<MonitoredDatabase>();
        public NotificationSettings Notifications { get; set; } = new NotificationSettings();
    }

    public class MonitoredDatabase
    {
        public string Name { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class NotificationSettings
    {
        public EmailSettings Email { get; set; } = new EmailSettings();
        public SlowQueryThresholds SlowQueryThresholds { get; set; } = new SlowQueryThresholds();
    }

    public class EmailSettings
    {
        public bool Enabled { get; set; }
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public List<string> ToAddresses { get; set; } = new List<string>();
    }

    public class SlowQueryThresholds
    {
        public int Critical { get; set; } = 5000;
        public int Warning { get; set; } = 2000;
    }
} 