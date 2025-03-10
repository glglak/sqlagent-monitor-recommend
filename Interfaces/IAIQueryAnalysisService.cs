namespace SqlMonitor.Interfaces
{
    public interface IAIQueryAnalysisService
    {
        Task<string> AnalyzeQueryAsync(SlowQuery query, CancellationToken cancellationToken);
    }
} 