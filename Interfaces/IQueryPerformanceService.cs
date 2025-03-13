using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Interfaces
{
    public interface IQueryPerformanceService
    {
        Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(CancellationToken cancellationToken);
        Task<string> GenerateOptimizationSuggestionsAsync(SlowQuery slowQuery, CancellationToken cancellationToken);
        Task SaveSlowQueryAsync(SlowQuery query, SlowQuerySeverity severity, CancellationToken cancellationToken);
        Task<IEnumerable<SlowQueryHistory>> GetHistoricalSlowQueriesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
        Task<IEnumerable<SlowQuery>> GetSlowQueriesFromQueryStoreAsync(CancellationToken cancellationToken);
    }
} 