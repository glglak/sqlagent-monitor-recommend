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
    public class QueryPerformanceController : ControllerBase
    {
        private readonly IQueryPerformanceService _queryPerformanceService;
        private readonly IAIQueryAnalysisService _aiQueryAnalysisService;

        public QueryPerformanceController(
            IQueryPerformanceService queryPerformanceService,
            IAIQueryAnalysisService aiQueryAnalysisService)
        {
            _queryPerformanceService = queryPerformanceService;
            _aiQueryAnalysisService = aiQueryAnalysisService;
        }

        [HttpGet("slow-queries/current")]
        public async Task<ActionResult<IEnumerable<SlowQuery>>> GetCurrentSlowQueries(CancellationToken cancellationToken)
        {
            var slowQueries = await _queryPerformanceService.GetSlowQueriesAsync(cancellationToken);
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