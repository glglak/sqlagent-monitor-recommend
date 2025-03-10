namespace SqlMonitor.Interfaces
{
    public interface IQueryPerformanceService
    {
        Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(CancellationToken cancellationToken);
        Task<string> GenerateOptimizationSuggestionsAsync(SlowQuery slowQuery, CancellationToken cancellationToken);
        Task SaveSlowQueryAsync(SlowQuery slowQuery, CancellationToken cancellationToken);
        Task<IEnumerable<SlowQuery>> GetHistoricalSlowQueriesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    }
} 