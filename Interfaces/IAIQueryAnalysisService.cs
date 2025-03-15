using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Interfaces
{
    public interface IAIQueryAnalysisService
    {
        /// <summary>
        /// Optimizes a SQL query using AI
        /// </summary>
        Task<QueryOptimizationResult> OptimizeQueryAsync(string query, string databaseContext);
        
        // Add missing method referenced in QueryPerformanceService and DatabasesController
        Task<string> AnalyzeQueryAsync(string query, string databaseContext);
    }
} 