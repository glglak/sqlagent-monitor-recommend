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

        [HttpGet("slow-queries")]
        public async Task<ActionResult<IEnumerable<SlowQuery>>> GetSlowQueries(CancellationToken cancellationToken)
        {
            var slowQueries = await _queryPerformanceService.GetSlowQueriesAsync(cancellationToken);
            
            // Generate optimization suggestions for each query
            foreach (var query in slowQueries)
            {
                query.OptimizationSuggestions = await _queryPerformanceService.GenerateOptimizationSuggestionsAsync(query, cancellationToken);
            }
            
            return Ok(slowQueries);
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<string>> AnalyzeQuery([FromBody] SlowQuery query, CancellationToken cancellationToken)
        {
            var analysis = await _aiQueryAnalysisService.AnalyzeQueryAsync(query, cancellationToken);
            return Ok(new { Analysis = analysis });
        }
    }
} 