using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

namespace SqlMonitor.Services
{
    public class AIQueryAnalysisService : IAIQueryAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIQueryAnalysisService> _logger;
        private readonly AISettings _aiSettings;

        public AIQueryAnalysisService(
            HttpClient httpClient,
            IOptions<AISettings> aiSettings,
            ILogger<AIQueryAnalysisService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _aiSettings = aiSettings.Value;
        }

        public async Task<string> AnalyzeQueryAsync(SlowQuery query, CancellationToken cancellationToken)
        {
            try
            {
                return _aiSettings.Provider.ToLower() switch
                {
                    "azureopenai" => await AnalyzeWithAzureOpenAIAsync(query, cancellationToken),
                    "claude" => await AnalyzeWithClaudeAsync(query, cancellationToken),
                    _ => throw new ArgumentException($"Unsupported AI provider: {_aiSettings.Provider}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query with AI");
                return "Error analyzing query. Please check logs for details.";
            }
        }

        private async Task<string> AnalyzeWithAzureOpenAIAsync(SlowQuery query, CancellationToken cancellationToken)
        {
            var requestUrl = $"{_aiSettings.Endpoint}/openai/deployments/{_aiSettings.DeploymentName}/chat/completions?api-version=2023-05-15";
            
            var prompt = CreatePrompt(query);
            
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are an expert SQL query optimizer. Analyze the provided SQL query and suggest optimizations." },
                    new { role = "user", content = prompt }
                },
                max_tokens = _aiSettings.MaxTokens,
                temperature = _aiSettings.Temperature
            };
            
            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", _aiSettings.ApiKey);
            
            var response = await _httpClient.PostAsync(requestUrl, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            return responseObject
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No analysis available";
        }

        private async Task<string> AnalyzeWithClaudeAsync(SlowQuery query, CancellationToken cancellationToken)
        {
            var requestUrl = _aiSettings.Endpoint;
            
            var prompt = CreatePrompt(query);
            
            var requestBody = new
            {
                model = _aiSettings.ModelName,
                messages = new[]
                {
                    new { role = "system", content = "You are an expert SQL query optimizer. Analyze the provided SQL query and suggest optimizations." },
                    new { role = "user", content = prompt }
                },
                max_tokens = _aiSettings.MaxTokens,
                temperature = _aiSettings.Temperature
            };
            
            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _aiSettings.ApiKey);
            
            var response = await _httpClient.PostAsync(requestUrl, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            return responseObject
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "No analysis available";
        }

        private string CreatePrompt(SlowQuery query)
        {
            return $@"Please analyze this SQL query that is performing slowly:

SQL Query:
```
{query.QueryText}
```

Performance Metrics:
- Average Duration: {query.AverageDurationMs} ms
- Execution Count: {query.ExecutionCount}
- Database: {query.DatabaseName}

Query Plan (XML):
```xml
{query.QueryPlan}
```

Please provide:
1. An analysis of why this query might be slow
2. Specific optimization recommendations (indexes, query rewrites, etc.)
3. Any potential schema changes that might help
4. Explanation of the problematic parts of the execution plan

Format your response in clear sections with markdown formatting.";
        }
    }
} 