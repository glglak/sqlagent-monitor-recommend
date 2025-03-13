using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

namespace SqlMonitor.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly SqlServerSettings _settings;

        public EmailNotificationService(
            IOptions<SqlServerSettings> settings,
            ILogger<EmailNotificationService> logger)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task NotifySlowQueryAsync(SlowQuery query, SlowQuerySeverity severity, CancellationToken cancellationToken)
        {
            if (!_settings.Notifications.Email.Enabled)
            {
                _logger.LogInformation("Email notifications are disabled");
                return;
            }

            var subject = $"[{severity}] Slow SQL Query Detected in {query.DatabaseName}";
            var body = $@"
<html>
<body>
    <h2>Slow SQL Query Detected</h2>
    <p><strong>Severity:</strong> {severity}</p>
    <p><strong>Database:</strong> {query.DatabaseName}</p>
    <p><strong>Average Duration:</strong> {query.AverageDurationMs:F2} ms</p>
    <p><strong>Execution Count:</strong> {query.ExecutionCount}</p>
    <p><strong>Last Execution:</strong> {query.LastExecutionTime}</p>
    
    <h3>Query Text:</h3>
    <pre>{query.QueryText}</pre>
    
    <h3>Optimization Suggestions:</h3>
    <pre>{query.OptimizationSuggestions}</pre>
</body>
</html>";

            await SendEmailAsync(subject, body, cancellationToken);
        }

        public async Task NotifyIndexFragmentationAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
        {
            if (!_settings.Notifications.Email.Enabled)
            {
                _logger.LogInformation("Email notifications are disabled");
                return;
            }

            var subject = $"Index Fragmentation Detected in {indexInfo.DatabaseName}";
            var body = $@"
<html>
<body>
    <h2>Index Fragmentation Detected</h2>
    <p><strong>Database:</strong> {indexInfo.DatabaseName}</p>
    <p><strong>Schema:</strong> {indexInfo.SchemaName}</p>
    <p><strong>Table:</strong> {indexInfo.TableName}</p>
    <p><strong>Index:</strong> {indexInfo.IndexName}</p>
    <p><strong>Fragmentation:</strong> {indexInfo.FragmentationPercentage:F2}%</p>
    <p><strong>Page Count:</strong> {indexInfo.PageCount}</p>
    <p><strong>Last Reindexed:</strong> {indexInfo.LastReindexed}</p>
    <p><strong>Recommended Action:</strong> {indexInfo.ReindexType}</p>
</body>
</html>";

            await SendEmailAsync(subject, body, cancellationToken);
        }

        private async Task SendEmailAsync(string subject, string body, CancellationToken cancellationToken)
        {
            try
            {
                var emailSettings = _settings.Notifications.Email;
                
                using var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.Port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(emailSettings.FromAddress),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                foreach (var recipient in emailSettings.ToAddresses)
                {
                    message.To.Add(recipient);
                }

                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation($"Email notification sent: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification");
            }
        }
    }
} 