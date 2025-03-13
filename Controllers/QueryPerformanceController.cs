using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;
using Microsoft.Extensions.Logging;

namespace SqlMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class QueryPerformanceController : ControllerBase
    {
        private readonly IQueryPerformanceService _queryPerformanceService;
        private readonly IAIQueryAnalysisService _aiQueryAnalysisService;
        private readonly ILogger<QueryPerformanceController> _logger;

        public QueryPerformanceController(
            IQueryPerformanceService queryPerformanceService,
            IAIQueryAnalysisService aiQueryAnalysisService,
            ILogger<QueryPerformanceController> logger)
        {
            _queryPerformanceService = queryPerformanceService;
            _aiQueryAnalysisService = aiQueryAnalysisService;
            _logger = logger;
        }

        [HttpGet("slow-queries/current")]
        public async Task<ActionResult<IEnumerable<SlowQuery>>> GetCurrentSlowQueries(CancellationToken cancellationToken)
        {
            // Try DMVs first
            var slowQueries = await _queryPerformanceService.GetSlowQueriesAsync(cancellationToken);
            
            // If DMVs didn't return any results, try Query Store
            if (!slowQueries.Any())
            {
                _logger.LogInformation("No slow queries found using DMVs, trying Query Store...");
                slowQueries = await _queryPerformanceService.GetSlowQueriesFromQueryStoreAsync(cancellationToken);
            }
            
            return Ok(slowQueries);
        }

        [HttpGet("slow-queries/history")]
        public async Task<ActionResult<IEnumerable<SlowQueryHistory>>> GetSlowQueryHistory(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            CancellationToken cancellationToken)
        {
            var history = await _queryPerformanceService.GetHistoricalSlowQueriesAsync(
                startDate, 
                endDate, 
                cancellationToken);
            return Ok(history);
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<string>> AnalyzeQuery(
            [FromBody] SlowQuery query, 
            CancellationToken cancellationToken)
        {
            var analysis = await _aiQueryAnalysisService.AnalyzeQueryAsync(query, cancellationToken);
            return Ok(new { Analysis = analysis });
        }
    }
} 