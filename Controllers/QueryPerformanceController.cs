using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

namespace SqlMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class QueryPerformanceController : ControllerBase
    {
        private readonly ILogger<QueryPerformanceController> _logger;
        private readonly IQueryPerformanceService _queryPerformanceService;
        private readonly IAIQueryAnalysisService _aiQueryAnalysisService;

        public QueryPerformanceController(
            ILogger<QueryPerformanceController> logger,
            IQueryPerformanceService queryPerformanceService,
            IAIQueryAnalysisService aiQueryAnalysisService)
        {
            _logger = logger;
            _queryPerformanceService = queryPerformanceService;
            _aiQueryAnalysisService = aiQueryAnalysisService;
        }

        [HttpGet("{databaseName}/slowqueries")]
        public async Task<ActionResult<IEnumerable<SlowQuery>>> GetSlowQueries(string databaseName, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting slow queries for database: {DatabaseName}", databaseName);
            var slowQueries = await _queryPerformanceService.GetSlowQueriesAsync(databaseName, cancellationToken);
            return Ok(slowQueries);
        }

        [HttpGet("{databaseName}/history")]
        public async Task<ActionResult<IEnumerable<SlowQueryHistory>>> GetQueryHistory(string databaseName)
        {
            _logger.LogInformation("Getting query history for database: {DatabaseName}", databaseName);
            var history = await _queryPerformanceService.GetHistoricalSlowQueriesAsync(databaseName);
            return Ok(history);
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<string>> AnalyzeQuery([FromBody] Models.QueryAnalysisRequest request)
        {
            _logger.LogInformation("Analyzing query for database: {DatabaseName}", request.DatabaseName);
            var analysis = await _aiQueryAnalysisService.AnalyzeQueryAsync(request.Query, request.DatabaseName);
            return Ok(analysis);
        }

        [HttpPost("{databaseName}/fix/{queryId}")]
        public async Task<ActionResult<QueryFixResult>> ApplyQueryFix(
            string databaseName, 
            string queryId, 
            [FromQuery] string fixType, 
            [FromBody] Models.QueryFixRequest request)
        {
            _logger.LogInformation("Applying fix {FixType} to query {QueryId} in database {DatabaseName}", 
                fixType, queryId, databaseName);
            
            var result = await _queryPerformanceService.ApplyQueryFixAsync(
                databaseName, queryId, fixType, request.Query);
                
            return Ok(result);
        }
    }
} 