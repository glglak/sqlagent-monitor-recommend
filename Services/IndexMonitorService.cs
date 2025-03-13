using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

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
    }
} 