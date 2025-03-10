using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SqlMonitor.Models;
using SqlMonitor.Services;

namespace SqlMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IndexMonitorController : ControllerBase
    {
        private readonly IIndexMonitorService _indexMonitorService;

        public IndexMonitorController(IIndexMonitorService indexMonitorService)
        {
            _indexMonitorService = indexMonitorService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IndexInfo>>> GetFragmentedIndexes(CancellationToken cancellationToken)
        {
            var indexes = await _indexMonitorService.GetFragmentedIndexesAsync(cancellationToken);
            return Ok(indexes);
        }

        [HttpPost("{databaseName}/{schemaName}/{tableName}/{indexName}/reindex")]
        public async Task<ActionResult> ReindexAsync(
            string databaseName, 
            string schemaName, 
            string tableName, 
            string indexName, 
            CancellationToken cancellationToken)
        {
            var indexInfo = new IndexInfo
            {
                DatabaseName = databaseName,
                SchemaName = schemaName,
                TableName = tableName,
                IndexName = indexName
            };
            
            await _indexMonitorService.ReindexAsync(indexInfo, cancellationToken);
            return Ok();
        }
    }
} 