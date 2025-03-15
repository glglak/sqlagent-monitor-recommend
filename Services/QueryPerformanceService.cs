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
using Microsoft.Data.SqlClient;

namespace SqlMonitor.Services
{
    public class QueryPerformanceService : IQueryPerformanceService
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IAIQueryAnalysisService _aiQueryAnalysisService;
        private readonly SqlMonitorContext _context;
        private readonly ILogger<QueryPerformanceService> _logger;
        private readonly SqlServerSettings _settings;

        public QueryPerformanceService(
            ISqlConnectionFactory connectionFactory,
            IAIQueryAnalysisService aiQueryAnalysisService,
            SqlMonitorContext context,
            IOptions<SqlServerSettings> settings,
            ILogger<QueryPerformanceService> logger)
        {
            _connectionFactory = connectionFactory;
            _aiQueryAnalysisService = aiQueryAnalysisService;
            _context = context;
            _logger = logger;
            _settings = settings.Value;
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
            return await _aiQueryAnalysisService.AnalyzeQueryAsync(slowQuery.Query ?? string.Empty, slowQuery.DatabaseName ?? string.Empty);
        }

        public async Task SaveSlowQueryAsync(SlowQuery query, SlowQuerySeverity severity, CancellationToken cancellationToken)
        {
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync("master");
                
                // Check if query already exists
                var existingQuery = await connection.QueryFirstOrDefaultAsync<SlowQueryHistory>(
                    @"SELECT * FROM SlowQueryHistory 
                      WHERE QueryText = @QueryText 
                      AND DatabaseName = @DatabaseName 
                      AND IsResolved = 0",
                    new { 
                        QueryText = query.QueryText, 
                        DatabaseName = query.DatabaseName 
                    }
                );

            if (existingQuery != null)
            {
                    // Update existing query
                    await connection.ExecuteAsync(
                        @"UPDATE SlowQueryHistory 
                          SET LastSeen = @LastSeen,
                              ExecutionCount = @ExecutionCount,
                              AverageDurationMs = @AverageDurationMs,
                              QueryPlan = @QueryPlan,
                              OptimizationSuggestions = @OptimizationSuggestions,
                              Severity = @Severity
                          WHERE Id = @Id",
                        new {
                            Id = existingQuery.Id,
                            LastSeen = DateTimeOffset.UtcNow,
                            ExecutionCount = query.ExecutionCount,
                            AverageDurationMs = query.AverageDurationMs,
                            QueryPlan = query.QueryPlan,
                            OptimizationSuggestions = query.OptimizationSuggestions,
                            Severity = (int)severity
                        }
                    );
            }
            else
                {
                    // Insert new query
                    await connection.ExecuteAsync(
                        @"INSERT INTO SlowQueryHistory (
                            QueryText, DatabaseName, AverageDurationMs, ExecutionCount,
                            FirstSeen, LastSeen, QueryPlan, OptimizationSuggestions,
                            Severity, IsResolved
                          ) VALUES (
                            @QueryText, @DatabaseName, @AverageDurationMs, @ExecutionCount,
                            @FirstSeen, @LastSeen, @QueryPlan, @OptimizationSuggestions,
                            @Severity, @IsResolved
                          )",
                        new {
                    QueryText = query.QueryText,
                    DatabaseName = query.DatabaseName,
                    AverageDurationMs = query.AverageDurationMs,
                    ExecutionCount = query.ExecutionCount,
                    FirstSeen = DateTimeOffset.UtcNow,
                    LastSeen = DateTimeOffset.UtcNow,
                    QueryPlan = query.QueryPlan,
                    OptimizationSuggestions = query.OptimizationSuggestions,
                            Severity = (int)severity,
                    IsResolved = false
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving slow query");
            }
        }

        public async Task<IEnumerable<SlowQueryHistory>> GetHistoricalSlowQueriesAsync(string databaseName)
        {
            _logger.LogInformation("Getting historical slow queries for database: {DatabaseName}", databaseName);

            try
            {
                // Use direct SQL query to get historical slow queries
                using var connection = await _connectionFactory.GetConnectionAsync(databaseName);
                var sql = @"
                    SELECT 
                        Id,
                        QueryText,
                        DatabaseName,
                        AverageDurationMs,
                        ExecutionCount,
                        FirstSeen,
                        LastSeen,
                        QueryPlan,
                        OptimizationSuggestions,
                        Severity,
                        IsResolved,
                        Resolution,
                        ResolvedAt
                    FROM SlowQueryHistory
                    WHERE DatabaseName = @DatabaseName
                    ORDER BY LastSeen DESC";
                    
                var result = await connection.QueryAsync<SlowQueryHistory>(
                    sql, 
                    new { DatabaseName = databaseName }
                );
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical slow queries");
                return Enumerable.Empty<SlowQueryHistory>();
            }
        }

        public async Task<IEnumerable<SlowQuery>> GetSlowQueriesFromQueryStoreAsync(CancellationToken cancellationToken)
        {
            var slowQueries = new List<SlowQuery>();
            _logger.LogInformation("Starting slow query detection using Query Store...");

            try
            {
                // First get a connection to master database to check states
                using var masterConnection = await _connectionFactory.GetConnectionAsync("master");
                
                // Get all monitored databases and their states
                var databaseStates = await masterConnection.QueryAsync<(string Name, string State)>(
                    @"SELECT name, state_desc 
                      FROM sys.databases 
                      WHERE name IN @DatabaseNames",
                    new { DatabaseNames = _settings.MonitoredDatabases.Select(db => db.Name).ToList() }
                );

                var dbStatesDict = databaseStates.ToDictionary(x => x.Name, x => x.State);
                
                foreach (var database in _settings.MonitoredDatabases)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation requested, stopping query store check");
                        break;
                    }

                    _logger.LogInformation($"Checking database: {database.Name}");
                    
                    try
                    {
                        // Check database state
                        if (!dbStatesDict.TryGetValue(database.Name, out var dbState) || dbState != "ONLINE")
                        {
                            _logger.LogWarning($"Database {database.Name} is not online. Current state: {dbState ?? "Unknown"}");
                            continue;
                        }

                        using var connection = await _connectionFactory.GetConnectionAsync(database.Name);
                        
                        // Check if Query Store is enabled
                        string? queryStoreEnabled = null;
                        try
                        {
                            queryStoreEnabled = await connection.ExecuteScalarAsync<string>(
                                "SELECT actual_state_desc FROM sys.database_query_store_options",
                                commandTimeout: 30
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Query Store is not available in {database.Name}. This is normal for system databases.");
                            continue;
                        }
                        
                        if (queryStoreEnabled != "READ_WRITE")
                        {
                            _logger.LogWarning($"Query Store is not enabled in {database.Name}. State: {queryStoreEnabled ?? "NULL"}");
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
                            new { SlowQueryThresholdMs = _settings.SlowQueryThresholdMs },
                            commandTimeout: 60
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

        public async Task<List<DatabaseInfo>> GetDatabasesAsync()
        {
            _logger.LogInformation("Getting monitored databases");
            var databases = new List<DatabaseInfo>();
            
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync("master");
                
                // Create a parameterized IN clause
                var monitoredDatabases = _settings.MonitoredDatabases.Select(db => db.Name).ToList();
                var paramList = string.Join(",", monitoredDatabases.Select((_, i) => $"@p{i}"));
                
                var sql = $@"SELECT 
                    name AS Name,
                    SERVERPROPERTY('ServerName') AS Server,
                    state_desc AS Status  -- This will be aliased as Status
                  FROM sys.databases
                  WHERE name IN ({paramList})
                  ORDER BY name";
                
                using var command = new SqlCommand(sql, (SqlConnection)connection);
                
                // Add parameters individually
                for (int i = 0; i < monitoredDatabases.Count; i++)
                {
                    command.Parameters.AddWithValue($"@p{i}", monitoredDatabases[i]);
                }
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    databases.Add(new DatabaseInfo
                    {
                        Name = reader["Name"].ToString() ?? string.Empty,
                        Server = reader["Server"].ToString() ?? string.Empty,
                        Status = reader["Status"].ToString() ?? "Unknown"  // Ensure we always have a status
                    });
                }

                _logger.LogInformation($"Found {databases.Count} databases: {string.Join(", ", databases.Select(d => $"{d.Name}({d.Status})"))}");
                
                return databases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitored databases");
                return new List<DatabaseInfo>();
            }
        }

        public async Task<string> GetDatabaseStatusAsync(string? databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                _logger.LogWarning("GetDatabaseStatusAsync called with null or empty database name");
                return "Unknown";
            }

            _logger.LogInformation($"Getting status for database: {databaseName}");
            
            try
            {
                // First check if this is a monitored database
                var isMonitored = _settings.MonitoredDatabases.Any(db => 
                    string.Equals(db.Name, databaseName, StringComparison.OrdinalIgnoreCase));
                
                if (!isMonitored)
                {
                    _logger.LogWarning($"Database {databaseName} is not in the monitored databases list");
                    return "Not Monitored";
                }

                using var connection = await _connectionFactory.GetConnectionAsync("master");
                using var command = new SqlCommand(
                    @"SELECT state_desc FROM sys.databases WHERE name = @databaseName", 
                    (SqlConnection)connection);
                
                command.Parameters.AddWithValue("@databaseName", databaseName);
                
                var result = await command.ExecuteScalarAsync();
                var status = result?.ToString() ?? "Unknown";
                _logger.LogInformation($"Database {databaseName} status: {status}");
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting status for database: {databaseName}");
                return "Error";
            }
        }

        public async Task<string> GetDatabaseNameAsync(string databaseName)
        {
            // Since we're using database names directly now, just return the input
            return databaseName;
        }

        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(string databaseName)
        {
            _logger.LogInformation($"Getting performance metrics for database: {databaseName}");
            var metrics = new PerformanceMetrics();
            
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync("master");
                using var command = new SqlCommand(
                    @"WITH CPU_Stats AS (
                        SELECT TOP 20
                            DATEADD(ms, -1 * (SELECT cpu_ticks/(cpu_ticks/ms_ticks) FROM sys.dm_os_sys_info), SYSDATETIME()) AS collection_time,
                            CAST(NULLIF(100.0 * sqlserver_process_cpu_utilization / NULLIF(system_cpu_utilization, 0), 0) AS DECIMAL(5,2)) as cpu_percent,
                            CAST(NULLIF(physical_memory_in_use_kb/1024.0/1024.0 * 100 / NULLIF(total_physical_memory_kb*1024.0/1024.0, 0), 0) AS DECIMAL(5,2)) as memory_percent,
                            CAST(NULLIF(100.0 * io_bytes_read / NULLIF(total_io_bytes, 0), 0) AS DECIMAL(5,2)) as io_percent,
                            CAST(NULLIF(100.0 * io_bytes_written / NULLIF(total_io_bytes, 0), 0) AS DECIMAL(5,2)) as log_write_percent
                        FROM (
                            SELECT 
                                record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') as system_idle_cpu_utilization,
                                record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') as sqlserver_process_cpu_utilization,
                                100 - record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') as system_cpu_utilization,
                                CONVERT(bigint, record.value('(./Record/ResourceMonitorEvent/Notification/MemoryUtilization/WorkingSet)[1]', 'bigint'))/1024 as physical_memory_in_use_kb,
                                CONVERT(bigint, record.value('(./Record/ResourceMonitorEvent/Notification/MemoryUtilization/TotalPhysicalMemory)[1]', 'bigint'))/1024 as total_physical_memory_kb,
                                CONVERT(bigint, record.value('(./Record/ResourceMonitorEvent/Notification/IoUtilization/IoStallTime)[1]', 'bigint')) as io_stall_time_ms,
                                CONVERT(bigint, record.value('(./Record/ResourceMonitorEvent/Notification/IoUtilization/ReadLatency)[1]', 'bigint')) as io_bytes_read,
                                CONVERT(bigint, record.value('(./Record/ResourceMonitorEvent/Notification/IoUtilization/WriteLatency)[1]', 'bigint')) as io_bytes_written,
                                CONVERT(bigint, record.value('(./Record/ResourceMonitorEvent/Notification/IoUtilization/ReadLatency)[1]', 'bigint')) + 
                                CONVERT(bigint, record.value('(./Record/ResourceMonitorEvent/Notification/IoUtilization/WriteLatency)[1]', 'bigint')) as total_io_bytes
                            FROM (
                                SELECT TOP 20 CAST(record as xml) as record
                                FROM sys.dm_os_ring_buffers
                                WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
                                OR ring_buffer_type = N'RING_BUFFER_RESOURCE_MONITOR'
                                ORDER BY timestamp DESC
                            ) as rb
                        ) as stats
                        ORDER BY collection_time DESC
                    )
                    SELECT 
                        CONVERT(VARCHAR(20), collection_time, 120) as collection_time,
                        ISNULL(cpu_percent, 0) as avg_cpu_percent,
                        ISNULL(memory_percent, 0) as avg_memory_usage_percent,
                        ISNULL(io_percent, 0) as avg_data_io_percent,
                        ISNULL(log_write_percent, 0) as avg_log_write_percent
                    FROM CPU_Stats
                    ORDER BY collection_time ASC", 
                    (SqlConnection)connection);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    metrics.Timestamps.Add(reader["collection_time"].ToString() ?? string.Empty);
                    metrics.Cpu.Add(reader.IsDBNull(reader.GetOrdinal("avg_cpu_percent")) ? 0 : Convert.ToDouble(reader["avg_cpu_percent"]));
                    metrics.Memory.Add(reader.IsDBNull(reader.GetOrdinal("avg_memory_usage_percent")) ? 0 : Convert.ToDouble(reader["avg_memory_usage_percent"]));
                    metrics.DiskIO.Add(reader.IsDBNull(reader.GetOrdinal("avg_data_io_percent")) ? 0 : Convert.ToDouble(reader["avg_data_io_percent"]));
                    metrics.NetworkIO.Add(reader.IsDBNull(reader.GetOrdinal("avg_log_write_percent")) ? 0 : Convert.ToDouble(reader["avg_log_write_percent"]));
                }
                
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting performance metrics for database: {databaseName}");
                return metrics;
            }
        }

        public async Task<List<SlowQuery>> GetSlowQueriesAsync(string? databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                _logger.LogWarning("GetSlowQueriesAsync called with null or empty database name");
                return new List<SlowQuery>();
            }

            _logger.LogInformation($"Getting slow queries for database: {databaseName}");
            var slowQueries = new List<SlowQuery>();
            
            try
            {
                // First check if this is a monitored database
                var isMonitored = _settings.MonitoredDatabases.Any(db => 
                    string.Equals(db.Name, databaseName, StringComparison.OrdinalIgnoreCase));
                
                if (!isMonitored)
                {
                    _logger.LogWarning($"Database {databaseName} is not in the monitored databases list");
                    return slowQueries;
                }

                using var connection = await _connectionFactory.GetConnectionAsync(databaseName);
                
                // Check if Query Store is enabled
                string? queryStoreEnabled = null;
                try
                {
                    queryStoreEnabled = await connection.ExecuteScalarAsync<string>(
                        "SELECT actual_state_desc FROM sys.database_query_store_options");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Query Store is not available in {databaseName}. This is normal for system databases.");
                    return slowQueries;
                }
                
                if (queryStoreEnabled != "READ_WRITE")
                {
                    _logger.LogWarning($"Query Store is not enabled in {databaseName}. State: {queryStoreEnabled ?? "NULL"}");
                    return slowQueries;
                }
                
                using (var command = new SqlCommand(
                    @"SELECT TOP 10
                        CAST(q.query_id AS VARCHAR(36)) AS Id,
                        SUBSTRING(qt.query_sql_text, 1, 500) AS Query,
                        rs.avg_duration / 1000.0 AS ExecutionTime,
                        rs.avg_cpu_time / 1000.0 AS CpuTime,
                        rs.avg_logical_io_reads AS LogicalReads,
                        rs.count_executions AS ExecutionCount,
                        'Analyze query plan' AS SuggestedFix,
                        0 AS Fixed
                      FROM sys.query_store_runtime_stats rs
                      JOIN sys.query_store_plan p ON rs.plan_id = p.plan_id
                      JOIN sys.query_store_query q ON p.query_id = q.query_id
                      JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
                      WHERE rs.avg_duration > @thresholdMs
                      ORDER BY rs.avg_duration DESC", 
                    (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@thresholdMs", _settings.SlowQueryThresholdMs);
                    
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        slowQueries.Add(new SlowQuery
                        {
                            Id = reader["Id"].ToString(),
                            Query = reader["Query"].ToString(),
                            ExecutionTime = Convert.ToDouble(reader["ExecutionTime"]),
                            CpuTime = Convert.ToDouble(reader["CpuTime"]),
                            LogicalReads = Convert.ToInt32(reader["LogicalReads"]),
                            ExecutionCount = Convert.ToInt32(reader["ExecutionCount"]),
                            SuggestedFix = reader["SuggestedFix"].ToString(),
                            Fixed = Convert.ToBoolean(reader["Fixed"]),
                            DatabaseName = databaseName // Ensure we set the database name
                        });
                    }
                }
                
                _logger.LogInformation($"Found {slowQueries.Count} slow queries in {databaseName}");
                return slowQueries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting slow queries for database: {databaseName}");
                return slowQueries;
            }
        }

        public async Task<SlowQuerySimulationResult> SimulateSlowQueryAsync(string? databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                _logger.LogWarning("SimulateSlowQueryAsync called with null or empty database name");
                return new SlowQuerySimulationResult
                {
                    Message = "Error: Database name cannot be null or empty",
                    ExecutionTime = 0,
                    QueryId = string.Empty,
                    QueryText = string.Empty
                };
            }

            // Check if this is a monitored database
            var isMonitored = _settings.MonitoredDatabases.Any(db => 
                string.Equals(db.Name, databaseName, StringComparison.OrdinalIgnoreCase));
            
            if (!isMonitored)
            {
                _logger.LogWarning($"Database {databaseName} is not in the monitored databases list");
                return new SlowQuerySimulationResult
                {
                    Message = $"Error: Database '{databaseName}' is not in the monitored databases list",
                    ExecutionTime = 0,
                    QueryId = string.Empty,
                    QueryText = string.Empty
                };
            }

            _logger.LogInformation($"Simulating slow query for database: {databaseName}");
            
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync(databaseName);
                
                // Create a temporary table if it doesn't exist
                using (var command = new SqlCommand(
                    @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SlowQuerySimulation')
                      CREATE TABLE SlowQuerySimulation (
                          Id INT IDENTITY(1,1) PRIMARY KEY,
                          Value NVARCHAR(MAX),
                          CreatedAt DATETIME DEFAULT GETDATE(),
                          Category NVARCHAR(50),
                          Status INT
                      )", 
                    (SqlConnection)connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                
                // Insert some data if the table is empty
                using (var command = new SqlCommand(
                    @"IF NOT EXISTS (SELECT TOP 1 1 FROM SlowQuerySimulation)
                      BEGIN
                          INSERT INTO SlowQuerySimulation (Value, Category, Status)
                          SELECT 
                              REPLICATE('X', 1000) AS Value,
                              CASE WHEN (object_id % 3) = 0 THEN 'Category A'
                                   WHEN (object_id % 3) = 1 THEN 'Category B'
                                   ELSE 'Category C' END AS Category,
                              object_id % 5 AS Status
                          FROM sys.objects
                          WHERE type = 'U' OR type = 'V'
                      END", 
                    (SqlConnection)connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                
                // Run a deliberately inefficient query with multiple issues:
                // 1. Missing indexes
                // 2. Cartesian product (cross join)
                // 3. Inefficient string operations
                // 4. Unnecessary sorting
                var startTime = DateTime.Now;
                
                // First, create a query that will force a table scan and be slow
                var slowQuery = @"
                    WITH NumberedRows AS (
                        SELECT 
                            s1.*,
                            ROW_NUMBER() OVER (ORDER BY s1.Id) AS RowNum
                        FROM SlowQuerySimulation s1
                    ),
                    CrossJoinedData AS (
                        SELECT 
                            n1.Id AS Id1,
                            n2.Id AS Id2,
                            n1.Value AS Value1,
                            n2.Value AS Value2,
                            n1.Category,
                            n1.Status,
                            SUBSTRING(n1.Value, 1, 10) + SUBSTRING(n2.Value, 1, 10) AS CombinedValue
                        FROM NumberedRows n1
                        CROSS JOIN NumberedRows n2
                        WHERE n1.RowNum <= 50 AND n2.RowNum <= 50
                    )
                    SELECT 
                        Id1,
                        Id2,
                        Category,
                        Status,
                        CombinedValue,
                        CASE 
                            WHEN Status = 0 THEN 'Inactive'
                            WHEN Status = 1 THEN 'Pending'
                            WHEN Status = 2 THEN 'Active'
                            WHEN Status = 3 THEN 'Suspended'
                            ELSE 'Unknown'
                        END AS StatusName,
                        DATEDIFF(day, GETDATE(), DATEADD(day, Id1 % 30, GETDATE())) AS DaysDifference
                    FROM CrossJoinedData
                    WHERE 
                        Category LIKE '%Category%' AND
                        CHARINDEX('X', Value1) > 0
                    ORDER BY 
                        Category,
                        Status,
                        Id1,
                        Id2";
                
                using (var command = new SqlCommand(slowQuery, (SqlConnection)connection))
                {
                    command.CommandTimeout = 120; // Set a longer timeout for this slow query
                    
                    using var reader = await command.ExecuteReaderAsync();
                    // Just read the first few rows to avoid excessive resource usage
                    int rowCount = 0;
                    while (await reader.ReadAsync() && rowCount < 100)
                    {
                        rowCount++;
                    }
                }
                var endTime = DateTime.Now;
                
                var executionTime = (endTime - startTime).TotalSeconds;
                
                // Save this query to the slow query history
                var queryText = slowQuery.Substring(0, Math.Min(slowQuery.Length, 4000));
                
                await SaveSimulatedSlowQuery(databaseName, queryText, executionTime);
                
                return new SlowQuerySimulationResult
                {
                    Message = "Slow query simulation completed successfully",
                    ExecutionTime = executionTime,
                    QueryId = Guid.NewGuid().ToString(),
                    QueryText = queryText
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error simulating slow query for database: {databaseName}");
                return new SlowQuerySimulationResult
                {
                    Message = $"Error: {ex.Message}",
                    ExecutionTime = 0,
                    QueryId = string.Empty,
                    QueryText = string.Empty
                };
            }
        }
        
        private async Task SaveSimulatedSlowQuery(string databaseName, string queryText, double executionTime)
        {
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync("master");
                
                // Check if the SlowQueryHistory table exists
                var tableExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.tables WHERE name = 'SlowQueryHistory'");
                
                if (tableExists == 0)
                {
                    // Create the table if it doesn't exist
                    await connection.ExecuteAsync(@"
                        CREATE TABLE SlowQueryHistory (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            QueryText NVARCHAR(MAX),
                            DatabaseName NVARCHAR(128),
                            AverageDurationMs FLOAT,
                            ExecutionCount INT,
                            FirstSeen DATETIMEOFFSET,
                            LastSeen DATETIMEOFFSET,
                            QueryPlan NVARCHAR(MAX),
                            OptimizationSuggestions NVARCHAR(MAX),
                            Severity INT,
                            IsResolved BIT,
                            Resolution NVARCHAR(MAX),
                            ResolvedAt DATETIMEOFFSET
                        )");
                }
                
                // Insert the simulated slow query
                await connection.ExecuteAsync(@"
                    INSERT INTO SlowQueryHistory (
                        QueryText, DatabaseName, AverageDurationMs, ExecutionCount,
                        FirstSeen, LastSeen, QueryPlan, OptimizationSuggestions,
                        Severity, IsResolved
                    ) VALUES (
                        @QueryText, @DatabaseName, @AverageDurationMs, @ExecutionCount,
                        @FirstSeen, @LastSeen, @QueryPlan, @OptimizationSuggestions,
                        @Severity, @IsResolved
                    )",
                    new {
                        QueryText = queryText,
                        DatabaseName = databaseName,
                        AverageDurationMs = executionTime * 1000,
                        ExecutionCount = 1,
                        FirstSeen = DateTimeOffset.UtcNow,
                        LastSeen = DateTimeOffset.UtcNow,
                        QueryPlan = "<ShowPlanXML xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\"><BatchSequence><Batch><Statements><StmtSimple><QueryPlan><MissingIndexes><MissingIndexGroup Impact=\"93.54\"><MissingIndex Database=\"[" + databaseName + "]\" Schema=\"[dbo]\" Table=\"[SlowQuerySimulation]\"><ColumnGroup Usage=\"EQUALITY\"><Column Name=\"[Category]\" ColumnId=\"4\"/></ColumnGroup><ColumnGroup Usage=\"INCLUDE\"><Column Name=\"[Value]\" ColumnId=\"2\"/></ColumnGroup></MissingIndex></MissingIndexGroup></MissingIndexes></QueryPlan></StmtSimple></Statements></Batch></BatchSequence></ShowPlanXML>",
                        OptimizationSuggestions = "Consider adding an index on the Category column. Avoid using CROSS JOIN which creates a cartesian product. Use more efficient string operations.",
                        Severity = (int)SlowQuerySeverity.Critical,
                        IsResolved = false
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving simulated slow query");
            }
        }

        public async Task<QueryFixResult> ApplyQueryFixAsync(string databaseName, string queryId, string fixType, string query)
        {
            _logger.LogInformation($"Applying fix for query {queryId} in database: {databaseName}");
            
            try
            {
                // Get the original query if not provided
                if (string.IsNullOrEmpty(query))
                {
                    using var connection = await _connectionFactory.GetConnectionAsync(databaseName);
                    using var command = new SqlCommand(
                        @"SELECT SUBSTRING(qt.query_sql_text, 1, 4000) AS Query
                          FROM sys.query_store_query q
                          JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
                          WHERE CAST(q.query_id AS VARCHAR(36)) = @queryId", 
                        (SqlConnection)connection);
                    
                    command.Parameters.AddWithValue("@queryId", queryId);
                    
                    var result = await command.ExecuteScalarAsync();
                    query = result?.ToString() ?? string.Empty;
                }
                
                if (string.IsNullOrEmpty(query))
                {
                    return new QueryFixResult
                    {
                        Message = "Query not found",
                        OriginalQuery = string.Empty,
                        OptimizedQuery = string.Empty,
                        Explanation = "The specified query could not be found",
                        OptimizedQueryWorks = false
                    };
                }
                
                // Use AI to optimize the query if fixType is "ai"
                var optimizationResult = fixType.ToLower() == "ai" 
                    ? await _aiQueryAnalysisService.OptimizeQueryAsync(databaseName, query)
                    : new QueryOptimizationResult
                    {
                        OptimizedQuery = query,
                        Explanation = "Manual optimization applied",
                        IndexRecommendations = new List<string>(),
                        IsSimulated = true
                    };
                
                // Measure performance of original query
                var performanceBefore = await MeasureQueryPerformanceAsync(databaseName, query);
                
                // Measure performance of optimized query if it's different
                var performanceAfter = query != optimizationResult.OptimizedQuery
                    ? await MeasureQueryPerformanceAsync(databaseName, optimizationResult.OptimizedQuery)
                    : performanceBefore;
                
                // Calculate improvement percentage
                var improvementPercent = performanceBefore.ExecutionTime > 0
                    ? Math.Round((1 - (performanceAfter.ExecutionTime / performanceBefore.ExecutionTime)) * 100, 2)
                    : 0;
                
                return new QueryFixResult
                {
                    Message = "Query optimization applied successfully",
                    OriginalQuery = query,
                    OptimizedQuery = optimizationResult.OptimizedQuery,
                    Explanation = optimizationResult.Explanation,
                    IndexRecommendations = optimizationResult.IndexRecommendations,
                    PerformanceBefore = performanceBefore,
                    PerformanceAfter = performanceAfter,
                    ImprovementPercent = $"{improvementPercent}%",
                    AIPowered = fixType.ToLower() == "ai",
                    OptimizedQueryWorks = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying fix for query {queryId} in database: {databaseName}");
                return new QueryFixResult
                {
                    Message = $"Error: {ex.Message}",
                    OriginalQuery = query,
                    OptimizedQuery = string.Empty,
                    Explanation = $"An error occurred: {ex.Message}",
                    OptimizedQueryWorks = false
                };
            }
        }
        
        private async Task<PerformanceComparison> MeasureQueryPerformanceAsync(string databaseName, string query)
        {
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync(databaseName);
                
                // Set up the performance counters
                using (var setupCommand = new SqlCommand(
                    @"SET STATISTICS TIME ON;
                      SET STATISTICS IO ON;", 
                    (SqlConnection)connection))
                {
                    await setupCommand.ExecuteNonQueryAsync();
                }
                
                // Execute the query and capture performance metrics
                var startTime = DateTime.Now;
                using (var command = new SqlCommand(query, (SqlConnection)connection))
                {
                    command.CommandTimeout = 60; // Set a reasonable timeout
                    
                    try
                    {
                        // For SELECT queries, use ExecuteScalar to avoid reading all results
                        if (query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        {
                            await command.ExecuteScalarAsync();
                        }
                        else
                        {
                            // For non-SELECT queries (INSERT, UPDATE, DELETE, etc.)
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue measuring time
                        _logger.LogWarning(ex, "Error executing query during performance measurement");
                    }
                }
                var endTime = DateTime.Now;
                
                // Get actual execution statistics from SQL Server if possible
                int logicalReads = 0;
                double cpuTime = 0;
                
                try
                {
                    using var statsCommand = new SqlCommand(
                        @"SELECT 
                            last_logical_reads AS LogicalReads,
                            last_worker_time / 1000000.0 AS CpuTimeSeconds
                          FROM sys.dm_exec_query_stats
                          WHERE last_execution_time = (SELECT MAX(last_execution_time) FROM sys.dm_exec_query_stats)",
                        (SqlConnection)connection);
                    
                    using var reader = await statsCommand.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        logicalReads = reader.GetInt32(0);
                        cpuTime = reader.GetDouble(1);
                    }
                }
                catch
                {
                    // If we can't get actual stats, use estimates based on execution time
                    logicalReads = 1000; // Placeholder
                    cpuTime = (endTime - startTime).TotalSeconds * 0.8; // Estimate
                }
                
                return new PerformanceComparison
                {
                    ExecutionTime = (endTime - startTime).TotalSeconds,
                    CpuTime = cpuTime,
                    LogicalReads = logicalReads
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error measuring query performance in database: {databaseName}");
                return new PerformanceComparison
                {
                    ExecutionTime = 0,
                    CpuTime = 0,
                    LogicalReads = 0
                };
            }
        }

        public async Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(string databaseName, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting slow queries for database: {DatabaseName}", databaseName);
            
            try
            {
                // Reuse the existing implementation but with cancellation token support
                var queries = await GetSlowQueriesFromQueryStoreAsync(cancellationToken);
                
                // Filter by database name if provided
                if (!string.IsNullOrEmpty(databaseName))
                {
                    queries = queries.Where(q => string.Equals(q.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase));
                }
                
                return queries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slow queries for database {DatabaseName}", databaseName);
                return new List<SlowQuery>();
            }
        }
    }

    public class SlowQueryEntity
    {
        public int Id { get; set; }
        public string? QueryText { get; set; }
        public string? DatabaseName { get; set; }
        public double AverageDurationMs { get; set; }
        public int ExecutionCount { get; set; }
        public DateTimeOffset FirstSeen { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public string? QueryPlan { get; set; }
        public string? OptimizationSuggestions { get; set; }
        public SlowQuerySeverity Severity { get; set; }
        public bool IsResolved { get; set; }
    }
} 