using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Interfaces
{
    public interface IQueryPerformanceService
    {
        /// <summary>
        /// Gets a list of all databases
        /// </summary>
        Task<List<DatabaseInfo>> GetDatabasesAsync();
        
        /// <summary>
        /// Gets the status of a database
        /// </summary>
        Task<string> GetDatabaseStatusAsync(string databaseId);
        
        /// <summary>
        /// Gets the database name from its ID
        /// </summary>
        Task<string> GetDatabaseNameAsync(string databaseId);
        
        /// <summary>
        /// Gets performance metrics for a database
        /// </summary>
        Task<PerformanceMetrics> GetPerformanceMetricsAsync(string databaseName);
        
        /// <summary>
        /// Gets slow queries for a database
        /// </summary>
        Task<List<SlowQuery>> GetSlowQueriesAsync(string databaseName);
        
        /// <summary>
        /// Simulates a slow query on a database
        /// </summary>
        Task<SlowQuerySimulationResult> SimulateSlowQueryAsync(string databaseName);
        
        /// <summary>
        /// Applies a fix to a slow query
        /// </summary>
        Task<QueryFixResult> ApplyQueryFixAsync(string databaseName, string queryId, string fixType, string query);

        /// <summary>
        /// Gets slow queries from Query Store
        /// </summary>
        Task<IEnumerable<SlowQuery>> GetSlowQueriesFromQueryStoreAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets slow queries for a database with cancellation support
        /// </summary>
        Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(string databaseName, CancellationToken cancellationToken);

        /// <summary>
        /// Gets slow queries with cancellation support
        /// </summary>
        Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets historical slow queries for a database
        /// </summary>
        Task<IEnumerable<SlowQueryHistory>> GetHistoricalSlowQueriesAsync(string databaseName);

        /// <summary>
        /// Generates optimization suggestions for a slow query
        /// </summary>
        Task<string> GenerateOptimizationSuggestionsAsync(SlowQuery slowQuery, CancellationToken cancellationToken);

        /// <summary>
        /// Saves a slow query with severity information
        /// </summary>
        Task SaveSlowQueryAsync(SlowQuery query, SlowQuerySeverity severity, CancellationToken cancellationToken);
    }
} 