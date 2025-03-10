using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Models;

namespace SqlMonitor.Services
{
    public class QueryPerformanceService : IQueryPerformanceService
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IAIQueryAnalysisService _aiQueryAnalysisService;
        private readonly ILogger<QueryPerformanceService> _logger;
        private readonly SqlServerSettings _settings;

        public QueryPerformanceService(
            ISqlConnectionFactory connectionFactory,
            IAIQueryAnalysisService aiQueryAnalysisService,
            IOptions<SqlServerSettings> settings,
            ILogger<QueryPerformanceService> logger)
        {
            _connectionFactory = connectionFactory;
            _aiQueryAnalysisService = aiQueryAnalysisService;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(CancellationToken cancellationToken)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            
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

            return await connection.QueryAsync<SlowQuery>(sql, new { SlowQueryThresholdMs = _settings.SlowQueryThresholdMs });
        }

        public async Task<string> GenerateOptimizationSuggestionsAsync(SlowQuery slowQuery, CancellationToken cancellationToken)
        {
            // Use AI to analyze the query
            return await _aiQueryAnalysisService.AnalyzeQueryAsync(slowQuery, cancellationToken);
        }
    }
} 