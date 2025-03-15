using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;

namespace SqlMonitor.Services
{
    public class IndexMonitorService : IIndexMonitorService
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<IndexMonitorService> _logger;
        private readonly SqlServerSettings _settings;

        public IndexMonitorService(
            ISqlConnectionFactory connectionFactory,
            IOptions<SqlServerSettings> settings,
            ILogger<IndexMonitorService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<IEnumerable<IndexInfo>> GetFragmentedIndexesAsync(CancellationToken cancellationToken)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            
            var sql = @"
                SELECT 
                    DB_NAME() AS DatabaseName,
                    SCHEMA_NAME(o.schema_id) AS SchemaName,
                    OBJECT_NAME(i.object_id) AS TableName,
                    i.name AS IndexName,
                    ips.avg_fragmentation_in_percent AS FragmentationPercentage,
                    ips.page_count AS PageCount,
                    STATS_DATE(i.object_id, i.index_id) AS LastReindexed
                FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
                JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
                JOIN sys.objects o ON i.object_id = o.object_id
                WHERE ips.avg_fragmentation_in_percent > @FragmentationThreshold
                AND ips.page_count > 100
                AND o.type = 'U'
                ORDER BY ips.avg_fragmentation_in_percent DESC";

            return await connection.QueryAsync<IndexInfo>(sql, new { FragmentationThreshold = _settings.IndexFragmentationThreshold });
        }

        public async Task ReindexAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            
            var reindexCommand = indexInfo.ReindexType == "REBUILD" 
                ? $"ALTER INDEX [{indexInfo.IndexName}] ON [{indexInfo.SchemaName}].[{indexInfo.TableName}] REBUILD WITH (ONLINE = ON)"
                : $"ALTER INDEX [{indexInfo.IndexName}] ON [{indexInfo.SchemaName}].[{indexInfo.TableName}] REORGANIZE";
            
            _logger.LogInformation($"Reindexing {indexInfo.DatabaseName}.{indexInfo.SchemaName}.{indexInfo.TableName}.{indexInfo.IndexName} with {indexInfo.ReindexType}");
            
            await connection.ExecuteAsync(reindexCommand);
            
            _logger.LogInformation($"Successfully reindexed {indexInfo.DatabaseName}.{indexInfo.SchemaName}.{indexInfo.TableName}.{indexInfo.IndexName}");
        }

        public async Task<List<MissingIndex>> GetMissingIndexesAsync(string databaseName)
        {
            _logger.LogInformation($"Getting missing indexes for database: {databaseName}");
            var missingIndexes = new List<MissingIndex>();
            
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync(databaseName);
                using var command = new SqlCommand(
                    @"SELECT 
                        CAST(ROW_NUMBER() OVER(ORDER BY avg_total_user_cost * avg_user_impact * (user_seeks + user_scans) DESC) AS VARCHAR(10)) AS Id,
                        statement AS Table,
                        equality_columns AS Columns,
                        included_columns AS IncludeColumns,
                        CAST(avg_total_user_cost * avg_user_impact * (user_seeks + user_scans) AS VARCHAR(20)) AS EstimatedImpact,
                        CAST(avg_user_impact AS INT) AS ImprovementPercent,
                        'CREATE INDEX IX_' + REPLACE(REPLACE(REPLACE(statement, '[', ''), ']', ''), '.', '_') + '_' + 
                        REPLACE(REPLACE(REPLACE(equality_columns, '[', ''), ']', ''), ', ', '_') + 
                        CASE WHEN included_columns IS NOT NULL THEN '_INCLUDE' ELSE '' END + 
                        ' ON ' + statement + ' (' + equality_columns + ')' + 
                        CASE WHEN included_columns IS NOT NULL THEN ' INCLUDE (' + included_columns + ')' ELSE '' END AS CreateStatement,
                        0 AS Created
                      FROM sys.dm_db_missing_index_details mid
                      INNER JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
                      INNER JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
                      WHERE database_id = DB_ID()
                      ORDER BY avg_total_user_cost * avg_user_impact * (user_seeks + user_scans) DESC", 
                    (SqlConnection)connection);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    missingIndexes.Add(new MissingIndex
                    {
                        Id = reader["Id"].ToString(),
                        Table = reader["Table"].ToString(),
                        Columns = reader["Columns"].ToString(),
                        IncludeColumns = reader["IncludeColumns"]?.ToString(),
                        EstimatedImpact = reader["EstimatedImpact"].ToString(),
                        ImprovementPercent = Convert.ToInt32(reader["ImprovementPercent"]),
                        CreateStatement = reader["CreateStatement"].ToString(),
                        Created = Convert.ToBoolean(reader["Created"])
                    });
                }
                
                return missingIndexes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting missing indexes for database: {databaseName}");
                return missingIndexes;
            }
        }

        public async Task<IndexCreationResult> SimulateIndexCreationAsync(string databaseName, string table, string columns, string? includeColumns = null)
        {
            _logger.LogInformation($"Simulating index creation for database: {databaseName}, table: {table}, columns: {columns}");
            
            try
            {
                // Generate a unique index name
                var indexName = $"IX_{table.Replace("[", "").Replace("]", "").Replace(".", "_")}_{columns.Replace("[", "").Replace("]", "").Replace(", ", "_")}";
                if (!string.IsNullOrEmpty(includeColumns))
                {
                    indexName += "_INCLUDE";
                }
                
                // Generate the CREATE INDEX statement
                var createStatement = $"CREATE INDEX {indexName} ON {table} ({columns})";
                if (!string.IsNullOrEmpty(includeColumns))
                {
                    createStatement += $" INCLUDE ({includeColumns})";
                }
                
                using var connection = await _connectionFactory.GetConnectionAsync(databaseName);
                
                // Check if the table exists
                using (var command = new SqlCommand(
                    @"SELECT COUNT(*) FROM sys.tables t
                      JOIN sys.schemas s ON t.schema_id = s.schema_id
                      WHERE s.name + '.' + t.name = @table
                      OR t.name = @tableName", 
                    (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@table", table.Replace("[", "").Replace("]", ""));
                    command.Parameters.AddWithValue("@tableName", table.Replace("[", "").Replace("]", "").Split('.').Last());
                    
                    var tableExists = (int)await command.ExecuteScalarAsync() > 0;
                    if (!tableExists)
                    {
                        return new IndexCreationResult
                        {
                            Message = $"Table {table} does not exist",
                            IndexName = indexName,
                            PerformanceImprovement = "0%"
                        };
                    }
                }
                
                // Check if the columns exist
                var columnList = columns.Split(',').Select(c => c.Trim().Replace("[", "").Replace("]", "")).ToList();
                if (!string.IsNullOrEmpty(includeColumns))
                {
                    columnList.AddRange(includeColumns.Split(',').Select(c => c.Trim().Replace("[", "").Replace("]", "")));
                }
                
                foreach (var column in columnList)
                {
                    using (var command = new SqlCommand(
                        @"SELECT COUNT(*) FROM sys.columns c
                          JOIN sys.tables t ON c.object_id = t.object_id
                          JOIN sys.schemas s ON t.schema_id = s.schema_id
                          WHERE c.name = @column
                          AND (s.name + '.' + t.name = @table OR t.name = @tableName)", 
                        (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@column", column);
                        command.Parameters.AddWithValue("@table", table.Replace("[", "").Replace("]", ""));
                        command.Parameters.AddWithValue("@tableName", table.Replace("[", "").Replace("]", "").Split('.').Last());
                        
                        var columnExists = (int)await command.ExecuteScalarAsync() > 0;
                        if (!columnExists)
                        {
                            return new IndexCreationResult
                            {
                                Message = $"Column {column} does not exist in table {table}",
                                IndexName = indexName,
                                PerformanceImprovement = "0%"
                            };
                        }
                    }
                }
                
                // Check if a similar index already exists
                using (var command = new SqlCommand(
                    @"SELECT COUNT(*) 
                      FROM sys.indexes i
                      JOIN sys.tables t ON i.object_id = t.object_id
                      JOIN sys.schemas s ON t.schema_id = s.schema_id
                      WHERE (s.name + '.' + t.name = @table OR t.name = @tableName)
                      AND i.name LIKE @indexPattern", 
                    (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@table", table.Replace("[", "").Replace("]", ""));
                    command.Parameters.AddWithValue("@tableName", table.Replace("[", "").Replace("]", "").Split('.').Last());
                    command.Parameters.AddWithValue("@indexPattern", $"%{columns.Replace("[", "").Replace("]", "").Replace(", ", "%")}%");
                    
                    var similarIndexExists = (int)await command.ExecuteScalarAsync() > 0;
                    if (similarIndexExists)
                    {
                        return new IndexCreationResult
                        {
                            Message = $"A similar index already exists on table {table}",
                            IndexName = indexName,
                            PerformanceImprovement = "0%"
                        };
                    }
                }
                
                // Get table size and estimate improvement based on column selectivity
                int tableRowCount = 0;
                int tablePageCount = 0;
                
                using (var command = new SqlCommand(
                    @"SELECT 
                        p.rows AS RowCount,
                        SUM(a.total_pages) AS PageCount
                      FROM sys.tables t
                      JOIN sys.schemas s ON t.schema_id = s.schema_id
                      JOIN sys.indexes i ON t.object_id = i.object_id
                      JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                      JOIN sys.allocation_units a ON p.partition_id = a.container_id
                      WHERE (s.name + '.' + t.name = @table OR t.name = @tableName)
                      GROUP BY p.rows", 
                    (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@table", table.Replace("[", "").Replace("]", ""));
                    command.Parameters.AddWithValue("@tableName", table.Replace("[", "").Replace("]", "").Split('.').Last());
                    
                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        tableRowCount = reader.GetInt32(0);
                        tablePageCount = reader.GetInt32(1);
                    }
                }
                
                // Calculate estimated improvement based on table size and column characteristics
                int improvementPercent = 0;
                
                if (tableRowCount > 0)
                {
                    // Check column selectivity for the first column (most important for index)
                    string firstColumn = columnList.First();
                    int distinctValues = 0;
                    
                    using (var command = new SqlCommand(
                        $"SELECT COUNT(DISTINCT [{firstColumn}]) FROM {table}", 
                        (SqlConnection)connection))
                    {
                        try
                        {
                            distinctValues = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }
                        catch
                        {
                            // If we can't get distinct count, estimate based on table size
                            distinctValues = Math.Max(1, tableRowCount / 10);
                        }
                    }
                    
                    // Calculate selectivity (0-1)
                    double selectivity = (double)distinctValues / tableRowCount;
                    
                    // Estimate improvement based on selectivity and table size
                    // High selectivity (close to 1) = better index performance
                    // Large tables benefit more from indexes
                    if (selectivity > 0.8)
                    {
                        // High selectivity - great candidate for an index
                        improvementPercent = Math.Min(90, 40 + (int)(tableRowCount / 10000));
                    }
                    else if (selectivity > 0.5)
                    {
                        // Medium selectivity
                        improvementPercent = Math.Min(70, 30 + (int)(tableRowCount / 20000));
                    }
                    else if (selectivity > 0.1)
                    {
                        // Low selectivity
                        improvementPercent = Math.Min(50, 20 + (int)(tableRowCount / 50000));
                    }
                    else
                    {
                        // Very low selectivity - poor candidate for an index
                        improvementPercent = Math.Min(30, 10 + (int)(tableRowCount / 100000));
                    }
                    
                    // Adjust for included columns (they can improve performance for certain queries)
                    if (!string.IsNullOrEmpty(includeColumns))
                    {
                        improvementPercent += 5;
                    }
                }
                else
                {
                    // If we can't determine table size, use a conservative estimate
                    improvementPercent = 25;
                }
                
                return new IndexCreationResult
                {
                    Message = $"Index creation simulation completed successfully. The index would improve query performance by approximately {improvementPercent}%.",
                    IndexName = indexName,
                    PerformanceImprovement = $"{improvementPercent}%"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error simulating index creation for database: {databaseName}");
                return new IndexCreationResult
                {
                    Message = $"Error: {ex.Message}",
                    IndexName = string.Empty,
                    PerformanceImprovement = "0%"
                };
            }
        }

        /// <summary>
        /// Checks for fragmented indexes and logs them
        /// </summary>
        public async Task CheckFragmentedIndexesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Checking for fragmented indexes");
            
            try
            {
                // Get fragmented indexes
                var fragmentedIndexes = await GetFragmentedIndexesAsync(cancellationToken);
                var fragmentedIndexesList = fragmentedIndexes.ToList();

                if (fragmentedIndexesList.Any())
                {
                    _logger.LogWarning($"Found {fragmentedIndexesList.Count} fragmented indexes");

                    // Group by database for logging
                    var indexesByDatabase = fragmentedIndexesList
                        .GroupBy(i => i.DatabaseName)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var dbGroup in indexesByDatabase)
                    {
                        _logger.LogWarning($"Found {dbGroup.Value.Count} fragmented indexes in database {dbGroup.Key}");

                        // Log details for each fragmented index
                        foreach (var index in dbGroup.Value)
                        {
                            _logger.LogInformation(
                                $"Fragmented index: {index.IndexName} on {index.TableName} " +
                                $"in {index.DatabaseName}, fragmentation: {index.FragmentationPercent}%, " +
                                $"page count: {index.PageCount}");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No fragmented indexes found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for fragmented indexes");
            }
        }

        /// <summary>
        /// Checks for missing indexes and logs them
        /// </summary>
        public async Task CheckMissingIndexesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Checking for missing indexes");
            
            try
            {
                // Check for missing indexes in each monitored database
                foreach (var db in _settings.MonitoredDatabases)
                {
                    var missingIndexes = await GetMissingIndexesAsync(db.Name);
                    
                    if (missingIndexes.Any())
                    {
                        _logger.LogWarning($"Found {missingIndexes.Count} missing indexes in {db.Name}");
                        
                        // Log significant missing indexes
                        var significantMissingIndexes = missingIndexes
                            .Where(i => i.ImprovementPercent >= 50)
                            .ToList();
                            
                        if (significantMissingIndexes.Any())
                        {
                            _logger.LogWarning($"Found {significantMissingIndexes.Count} significant missing indexes in {db.Name}");
                            foreach (var index in significantMissingIndexes)
                            {
                                _logger.LogInformation(
                                    $"Missing index on {index.Table}, columns: {index.Columns}, " +
                                    $"improvement: {index.ImprovementPercent}%, " +
                                    $"create statement: {index.CreateStatement}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"No missing indexes found in {db.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for missing indexes");
            }
        }
    }
} 