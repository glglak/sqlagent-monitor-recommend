using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

namespace SqlMonitor.Services
{
    // This service is no longer registered in the DI container
    // It's kept as a placeholder in case email notification functionality is needed in the future
    public class EmailNotificationService : INotificationService
    {
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(ILogger<EmailNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendErrorNotificationAsync(string subject, string message)
        {
            // Just log the message instead of sending an email
            _logger.LogWarning("Email notification would be sent: Subject: {subject}, Message: {message}", subject, message);
            return Task.CompletedTask;
        }
    }
} 