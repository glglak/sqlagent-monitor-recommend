using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;
using System.Collections.Generic;

namespace SqlMonitor.Interfaces
{
    // This interface is no longer used in the application
    // It's kept as a placeholder in case notification functionality is needed in the future
    public interface INotificationService
    {
        // Placeholder method
        Task SendErrorNotificationAsync(string subject, string message);
    }
} 