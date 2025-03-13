using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Interfaces
{
    public interface IAIQueryAnalysisService
    {
        Task<string> AnalyzeQueryAsync(SlowQuery query, CancellationToken cancellationToken);
    }
} 