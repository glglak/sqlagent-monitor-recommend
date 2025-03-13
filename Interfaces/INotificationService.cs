using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Interfaces
{
    public interface INotificationService
    {
        Task NotifySlowQueryAsync(SlowQuery query, SlowQuerySeverity severity, CancellationToken cancellationToken);
        Task NotifyIndexFragmentationAsync(IndexInfo indexInfo, CancellationToken cancellationToken);
    }
} 