using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Data;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

namespace SqlMonitor.Services
{
    public class QueryPerformanceService : IQueryPerformanceService
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IAIQueryAnalysisService _aiQueryAnalysisService;
        private readonly SqlMonitorContext _context;
        private readonly ILogger<QueryPerformanceService> _logger;
        private readonly SqlServerSettings _settings;
        private readonly INotificationService _notificationService;

        public QueryPerformanceService(
            ISqlConnectionFactory connectionFactory,
            IAIQueryAnalysisService aiQueryAnalysisService,
            SqlMonitorContext context,
            IOptions<SqlServerSettings> settings,
            INotificationService notificationService,
            ILogger<QueryPerformanceService> logger)
        {
            _connectionFactory = connectionFactory;
            _aiQueryAnalysisService = aiQueryAnalysisService;
            _context = context;
            _logger = logger;
            _settings = settings.Value;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(CancellationToken cancellationToken)
        {
            var slowQueries = new List<SlowQuery>();
            _logger.LogInformation("Starting slow query detection...");

            try
            {
                foreach (var database in _settings.MonitoredDatabases)
                {
                    _logger.LogInformation($"Checking database: {database.Name}");
                    
                    try
                    {
                        using var connection = await _connectionFactory.CreateConnectionAsync(database.ConnectionString);
                        
                        var sql = @"
                            SELECT TOP 20
                                qt.text AS QueryText,
                                DB_NAME(qp.dbid) AS DatabaseName,
                                qs.total_elapsed_time / qs.execution_count / 1000.0 AS AverageDurationMs,
                                qs.execution_count AS ExecutionCount,
                                qs.last_execution_time AS LastExecutionTime,
                                qp.query_plan AS QueryPlan
                            FROM sys.dm_exec_query_stats qs
                            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
                            CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
                            WHERE qs.total_elapsed_time / qs.execution_count > @SlowQueryThresholdMs * 1000
                            AND qt.text NOT LIKE '%sys.%'
                            ORDER BY qs.total_elapsed_time / qs.execution_count DESC";

                        _logger.LogInformation($"Executing query with threshold: {_settings.SlowQueryThresholdMs}ms");
                        
                        var databaseSlowQueries = await connection.QueryAsync<SlowQuery>(
                            sql, 
                            new { SlowQueryThresholdMs = _settings.SlowQueryThresholdMs }
                        );

                        _logger.LogInformation($"Found {databaseSlowQueries.Count()} slow queries in {database.Name}");
                        
                        slowQueries.AddRange(databaseSlowQueries);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error checking database {database.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSlowQueriesAsync");
            }

            _logger.LogInformation($"Total slow queries found: {slowQueries.Count}");
            return slowQueries;
        }

        public async Task<string> GenerateOptimizationSuggestionsAsync(SlowQuery slowQuery, CancellationToken cancellationToken)
        {
            return await _aiQueryAnalysisService.AnalyzeQueryAsync(slowQuery, cancellationToken);
        }

        public async Task SaveSlowQueryAsync(SlowQuery query, SlowQuerySeverity severity, CancellationToken cancellationToken)
        {
            var existingQuery = await _context.SlowQueries
                .FirstOrDefaultAsync(q => 
                    q.QueryText == query.QueryText && 
                    q.DatabaseName == query.DatabaseName &&
                    !q.IsResolved,
                    cancellationToken);

            if (existingQuery != null)
            {
                existingQuery.LastSeen = DateTimeOffset.UtcNow;
                existingQuery.ExecutionCount = query.ExecutionCount;
                existingQuery.AverageDurationMs = query.AverageDurationMs;
                existingQuery.QueryPlan = query.QueryPlan;
                existingQuery.OptimizationSuggestions = query.OptimizationSuggestions;
                existingQuery.Severity = severity;
            }
            else
            {
                await _context.SlowQueries.AddAsync(new SlowQueryHistory
                {
                    QueryText = query.QueryText,
                    DatabaseName = query.DatabaseName,
                    AverageDurationMs = query.AverageDurationMs,
                    ExecutionCount = query.ExecutionCount,
                    FirstSeen = DateTimeOffset.UtcNow,
                    LastSeen = DateTimeOffset.UtcNow,
                    QueryPlan = query.QueryPlan,
                    OptimizationSuggestions = query.OptimizationSuggestions,
                    Severity = severity,
                    IsResolved = false
                }, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<SlowQueryHistory>> GetHistoricalSlowQueriesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            return new List<SlowQueryHistory>();
        }

        public async Task<IEnumerable<SlowQuery>> GetSlowQueriesFromQueryStoreAsync(CancellationToken cancellationToken)
        {
            var slowQueries = new List<SlowQuery>();
            _logger.LogInformation("Starting slow query detection using Query Store...");

            try
            {
                foreach (var database in _settings.MonitoredDatabases)
                {
                    _logger.LogInformation($"Checking database: {database.Name}");
                    
                    try
                    {
                        using var connection = await _connectionFactory.CreateConnectionAsync(database.ConnectionString);
                        
                        // First check if Query Store is enabled
                        var queryStoreEnabled = await connection.ExecuteScalarAsync<string>(
                            "SELECT actual_state_desc FROM sys.database_query_store_options");
                        
                        if (queryStoreEnabled != "READ_WRITE")
                        {
                            _logger.LogWarning($"Query Store is not enabled in {database.Name}. State: {queryStoreEnabled}");
                            continue;
                        }
                        
                        var sql = @"
                            SELECT TOP 20
                                qt.query_sql_text AS QueryText,
                                DB_NAME() AS DatabaseName,
                                rs.avg_duration / 1000.0 AS AverageDurationMs,
                                rs.count_executions AS ExecutionCount,
                                rs.last_execution_time AS LastExecutionTime,
                                TRY_CONVERT(NVARCHAR(MAX), p.query_plan) AS QueryPlan
                            FROM sys.query_store_query q
                            JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
                            JOIN sys.query_store_plan p ON q.query_id = p.query_id
                            JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
                            WHERE rs.avg_duration / 1000.0 > @SlowQueryThresholdMs
                            AND qt.query_sql_text NOT LIKE '%sys.%'
                            ORDER BY rs.avg_duration DESC";

                        _logger.LogInformation($"Executing Query Store query with threshold: {_settings.SlowQueryThresholdMs}ms");
                        
                        var databaseSlowQueries = await connection.QueryAsync<SlowQuery>(
                            sql, 
                            new { SlowQueryThresholdMs = _settings.SlowQueryThresholdMs }
                        );

                        _logger.LogInformation($"Found {databaseSlowQueries.Count()} slow queries in {database.Name} using Query Store");
                        
                        slowQueries.AddRange(databaseSlowQueries);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error checking database {database.Name} using Query Store");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSlowQueriesFromQueryStoreAsync");
            }

            _logger.LogInformation($"Total slow queries found using Query Store: {slowQueries.Count}");
            return slowQueries;
        }
    }
} 