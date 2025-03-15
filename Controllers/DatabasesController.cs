using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SqlMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabasesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabasesController> _logger;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IQueryPerformanceService _queryPerformanceService;
        private readonly IIndexMonitorService _indexMonitorService;
        private readonly IAIQueryAnalysisService _aiQueryAnalysisService;

        public DatabasesController(
            IConfiguration configuration,
            ILogger<DatabasesController> logger,
            ISqlConnectionFactory connectionFactory,
            IQueryPerformanceService queryPerformanceService,
            IIndexMonitorService indexMonitorService,
            IAIQueryAnalysisService aiQueryAnalysisService)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionFactory = connectionFactory;
            _queryPerformanceService = queryPerformanceService;
            _indexMonitorService = indexMonitorService;
            _aiQueryAnalysisService = aiQueryAnalysisService;
        }

        // GET: api/databases
        [HttpGet]
        public async Task<IActionResult> GetDatabases()
        {
            try
            {
                _logger.LogInformation("Getting all databases");
                
                // Use the service to get databases instead of direct SQL
                var databases = await _queryPerformanceService.GetDatabasesAsync();
                
                _logger.LogInformation($"Found {databases.Count} databases");
                return Ok(databases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting databases");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/databases/{databaseId}/status
        [HttpGet("{databaseId}/status")]
        public async Task<IActionResult> GetDatabaseStatus(string databaseId)
        {
            if (string.IsNullOrEmpty(databaseId))
            {
                return BadRequest("Database ID is required");
            }

            try
            {
                _logger.LogInformation($"Checking status for database ID: {databaseId}");
                
                // Use the service to check database status
                var status = await _queryPerformanceService.GetDatabaseStatusAsync(databaseId);
                
                _logger.LogInformation($"Database ID {databaseId} status: {status}");
                return Ok(new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting status for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/databases/{databaseId}/performance
        [HttpGet("{databaseId}/performance")]
        public async Task<IActionResult> GetPerformanceMetrics(string databaseId)
        {
            try
            {
                _logger.LogInformation($"Getting performance metrics for database ID: {databaseId}");
                
                // Get the database name from the ID
                var dbName = await _queryPerformanceService.GetDatabaseNameAsync(databaseId);

                if (string.IsNullOrEmpty(dbName))
                {
                    _logger.LogWarning($"Database with ID {databaseId} not found");
                    return NotFound(new { error = "Database not found" });
                }

                // Get performance metrics from the service
                var metrics = await _queryPerformanceService.GetPerformanceMetricsAsync(dbName);
                
                _logger.LogInformation($"Retrieved performance metrics for database {dbName}");
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting performance metrics for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/databases/{databaseId}/slowqueries
        [HttpGet("{databaseId}/slowqueries")]
        public async Task<IActionResult> GetSlowQueries(string databaseId)
        {
            try
            {
                _logger.LogInformation($"Getting slow queries for database ID: {databaseId}");
                
                // Get the database name from the ID
                var dbName = await _queryPerformanceService.GetDatabaseNameAsync(databaseId);

                if (string.IsNullOrEmpty(dbName))
                {
                    _logger.LogWarning($"Database with ID {databaseId} not found");
                    return NotFound(new { error = "Database not found" });
                }

                // Get slow queries from the service
                var slowQueries = await _queryPerformanceService.GetSlowQueriesAsync(dbName);
                
                _logger.LogInformation($"Retrieved slow queries for database {dbName}");
                return Ok(slowQueries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting slow queries for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/databases/{databaseId}/missingindexes
        [HttpGet("{databaseId}/missingindexes")]
        public async Task<IActionResult> GetMissingIndexes(string databaseId)
        {
            try
            {
                _logger.LogInformation($"Getting missing indexes for database ID: {databaseId}");
                
                // Get the database name from the ID
                var dbName = await _queryPerformanceService.GetDatabaseNameAsync(databaseId);

                if (string.IsNullOrEmpty(dbName))
                {
                    _logger.LogWarning($"Database with ID {databaseId} not found");
                    return NotFound(new { error = "Database not found" });
                }

                // Get missing indexes from the service
                var missingIndexes = await _indexMonitorService.GetMissingIndexesAsync(dbName);
                
                _logger.LogInformation($"Retrieved missing indexes for database {dbName}");
                return Ok(missingIndexes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting missing indexes for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/databases/{databaseId}/simulate/slowquery
        [HttpPost("{databaseId}/simulate/slowquery")]
        public async Task<IActionResult> SimulateSlowQuery(string databaseId)
        {
            try
            {
                _logger.LogInformation($"Simulating slow query for database ID: {databaseId}");
                
                // Get the database name from the ID
                var dbName = await _queryPerformanceService.GetDatabaseNameAsync(databaseId);

                if (string.IsNullOrEmpty(dbName))
                {
                    _logger.LogWarning($"Database with ID {databaseId} not found");
                    return NotFound(new { error = "Database not found" });
                }

                // Simulate a slow query
                var result = await _queryPerformanceService.SimulateSlowQueryAsync(dbName);
                
                _logger.LogInformation($"Simulated slow query for database {dbName}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error simulating slow query for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/databases/{databaseId}/simulate/createindex
        [HttpPost("{databaseId}/simulate/createindex")]
        public async Task<IActionResult> SimulateCreateIndex(string databaseId, [FromBody] IndexCreationRequest request)
        {
            try
            {
                _logger.LogInformation($"Simulating index creation for database ID: {databaseId}");
                
                if (request == null || string.IsNullOrEmpty(request.Table) || string.IsNullOrEmpty(request.Columns))
                {
                    _logger.LogWarning("Invalid index creation request");
                    return BadRequest(new { error = "Table and columns are required" });
                }
                
                // Get the database name from the ID
                var dbName = await _queryPerformanceService.GetDatabaseNameAsync(databaseId);

                if (string.IsNullOrEmpty(dbName))
                {
                    _logger.LogWarning($"Database with ID {databaseId} not found");
                    return NotFound(new { error = "Database not found" });
                }

                // Simulate index creation
                var result = await _indexMonitorService.SimulateIndexCreationAsync(dbName, request.Table, request.Columns, request.IncludeColumns);
                
                _logger.LogInformation($"Simulated index creation for database {dbName}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error simulating index creation for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/databases/{databaseName}/queries/{queryId}/fix
        [HttpPost("{databaseName}/queries/{queryId}/fix")]
        public async Task<IActionResult> ApplyQueryFix(string databaseName, string queryId, [FromBody] QueryFixRequest request)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return BadRequest("Database name is required");
            }

            if (string.IsNullOrEmpty(queryId))
            {
                return BadRequest("Query ID is required");
            }

            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            var result = await _queryPerformanceService.ApplyQueryFixAsync(
                databaseName,
                queryId,
                request.FixType ?? "manual",
                request.Query ?? string.Empty
            );

            return Ok(result);
        }

        // POST: api/databases/{databaseId}/optimize
        [HttpPost("{databaseId}/optimize")]
        public async Task<IActionResult> OptimizeQuery(string databaseId, [FromBody] QueryOptimizationRequest request)
        {
            try
            {
                _logger.LogInformation($"Optimizing query with AI for database ID: {databaseId}");
                
                if (request == null || string.IsNullOrEmpty(request.Query))
                {
                    _logger.LogWarning("Invalid query optimization request");
                    return BadRequest(new { error = "Query is required" });
                }
                
                // Get the database name from the ID
                var dbName = await _queryPerformanceService.GetDatabaseNameAsync(databaseId);

                if (string.IsNullOrEmpty(dbName))
                {
                    _logger.LogWarning($"Database with ID {databaseId} not found");
                    return NotFound(new { error = "Database not found" });
                }

                // Check if Azure OpenAI is configured
                var aiSettings = _configuration.GetSection("AI").Get<AISettings>();
                if (aiSettings == null || string.IsNullOrEmpty(aiSettings.ApiKey))
                {
                    _logger.LogWarning("Azure OpenAI is not configured");
                    return BadRequest(new { 
                        OptimizedQuery = request.Query,
                        Explanation = "Azure OpenAI is not configured. Please add your Azure OpenAI credentials to the configuration.",
                        IndexRecommendations = new List<string>(),
                        IsSimulated = true,
                        PerformanceMetrics = new {},
                        ImprovementPercent = 0
                    });
                }

                // Optimize query with AI
                var result = await _aiQueryAnalysisService.OptimizeQueryAsync(request.Query, dbName);
                
                _logger.LogInformation($"Optimized query with AI for database {dbName}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error optimizing query with AI for database {databaseId}");
                return StatusCode(500, new { 
                    OptimizedQuery = request.Query,
                    Explanation = $"Error optimizing query: {ex.Message}",
                    IndexRecommendations = new List<string>(),
                    IsSimulated = false,
                    PerformanceMetrics = new {},
                    ImprovementPercent = 0
                });
            }
        }

        [HttpGet("test-ai-connection")]
        public async Task<IActionResult> TestAIConnection()
        {
            try
            {
                _logger.LogInformation("Testing Azure OpenAI connection");
                
                var aiSettings = _configuration.GetSection("AI").Get<AISettings>();
                if (aiSettings == null || string.IsNullOrEmpty(aiSettings.ApiKey))
                {
                    return BadRequest("Azure OpenAI settings are not configured");
                }

                // Log the configuration we're using
                _logger.LogInformation("Testing connection with settings: Endpoint={Endpoint}, DeploymentName={DeploymentName}, ModelName={ModelName}, ApiVersion={ApiVersion}",
                    aiSettings.Endpoint,
                    aiSettings.DeploymentName,
                    aiSettings.ModelName,
                    aiSettings.ApiVersion);

                // First test a simple chat completion
                var testMessage = "Test connection to Azure OpenAI. Respond with 'Connection successful.'";
                var result = await _aiQueryAnalysisService.AnalyzeQueryAsync(testMessage, "test");
                
                // Log the full URL being used
                var testUrl = $"{aiSettings.Endpoint}/openai/deployments/{aiSettings.DeploymentName}/chat/completions?api-version={aiSettings.ApiVersion}";
                _logger.LogInformation("Test URL: {Url}", testUrl);

                return Ok(new { 
                    success = true,
                    message = "Connection successful",
                    endpoint = aiSettings.Endpoint,
                    deploymentName = aiSettings.DeploymentName,
                    modelName = aiSettings.ModelName,
                    apiVersion = aiSettings.ApiVersion,
                    testUrl = testUrl,
                    result = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Azure OpenAI connection");
                return StatusCode(500, new { 
                    success = false,
                    error = ex.Message,
                    details = ex.ToString(),
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }

    public class IndexCreationRequest
    {
        public string? Table { get; set; }
        public string? Columns { get; set; }
        public string? IncludeColumns { get; set; }
    }

    public class QueryFixRequest
    {
        public string? FixType { get; set; }
        public string? Query { get; set; }
    }

    public class QueryOptimizationRequest
    {
        public string? DatabaseId { get; set; }
        public string? Query { get; set; }
    }
} 