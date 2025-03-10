using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Services
{
    public interface IQueryPerformanceService
    {
        Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(CancellationToken cancellationToken);
        Task<string> GenerateOptimizationSuggestionsAsync(SlowQuery slowQuery, CancellationToken cancellationToken);
    }
} 