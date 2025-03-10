using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Models;
using Microsoft.EntityFrameworkCore;

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

            foreach (var database in _settings.MonitoredDatabases)
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

                var databaseSlowQueries = await connection.QueryAsync<SlowQuery>(
                    sql, 
                    new { SlowQueryThresholdMs = _settings.SlowQueryThresholdMs }
                );

                slowQueries.AddRange(databaseSlowQueries);

                // Process and store each slow query
                foreach (var query in databaseSlowQueries)
                {
                    await ProcessSlowQueryAsync(query, cancellationToken);
                }
            }

            return slowQueries;
        }

        private async Task ProcessSlowQueryAsync(SlowQuery query, CancellationToken cancellationToken)
        {
            // Get AI analysis
            query.OptimizationSuggestions = await _aiQueryAnalysisService.AnalyzeQueryAsync(query, cancellationToken);

            // Determine severity
            var severity = DetermineSeverity(query.AverageDurationMs);

            // Save to database
            await SaveSlowQueryAsync(query, severity, cancellationToken);

            // Send notifications if needed
            if (severity >= SlowQuerySeverity.Warning)
            {
                await _notificationService.NotifySlowQueryAsync(query, severity, cancellationToken);
            }
        }

        private SlowQuerySeverity DetermineSeverity(double durationMs)
        {
            if (durationMs >= _settings.Notifications.SlowQueryThresholds.Critical)
                return SlowQuerySeverity.Critical;
            if (durationMs >= _settings.Notifications.SlowQueryThresholds.Warning)
                return SlowQuerySeverity.Warning;
            return SlowQuerySeverity.Normal;
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
                existingQuery.LastSeen = DateTime.UtcNow;
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
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    QueryPlan = query.QueryPlan,
                    OptimizationSuggestions = query.OptimizationSuggestions,
                    Severity = severity,
                    IsResolved = false
                }, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<SlowQueryHistory>> GetHistoricalSlowQueriesAsync(
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken)
        {
            return await _context.SlowQueries
                .Where(q => q.LastSeen >= startDate && q.LastSeen <= endDate)
                .OrderByDescending(q => q.LastSeen)
                .ToListAsync(cancellationToken);
        }
    }
} 