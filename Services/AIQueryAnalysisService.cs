using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private readonly AISettings _settings;
        private const int MaxRetries = 3;
        private const int InitialRetryDelayMs = 1000; // 1 second

        public AIQueryAnalysisService(
            HttpClient httpClient,
            ILogger<AIQueryAnalysisService> logger,
            IOptions<AISettings> settings)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // Increase timeout to 60 seconds
            _logger = logger;
            _settings = settings.Value;
            
            _logger.LogInformation("Initialized AIQueryAnalysisService with settings: Endpoint={Endpoint}, DeploymentName={DeploymentName}, ModelName={ModelName}, ApiVersion={ApiVersion}",
                _settings.Endpoint,
                _settings.DeploymentName,
                _settings.ModelName,
                _settings.ApiVersion);
        }

        public async Task<string> AnalyzeQueryAsync(string query, string databaseContext)
        {
            try
            {
                _logger.LogInformation("Analyzing query with AI: {Query}", query);
                _logger.LogInformation("Using endpoint: {Endpoint}, deployment: {Deployment}, api-version: {ApiVersion}", 
                    _settings.Endpoint, 
                    _settings.DeploymentName,
                    _settings.ApiVersion);

                string model = _settings.ModelName ?? "gpt-4";
                _logger.LogInformation("Using model: {Model}", model);

                var messages = new List<object>
                {
                    new { role = "system", content = "You are an expert SQL query optimizer. Analyze the provided SQL query and suggest optimizations." },
                    new { role = "user", content = $"Analyze this SQL query for a SQL Server database:\n\n{query}\n\nDatabase context: {databaseContext}" }
                };

                var requestData = new
                {
                    model = model,
                    messages = messages,
                    max_tokens = _settings.MaxTokens,
                    temperature = _settings.Temperature,
                    top_p = 1,
                    frequency_penalty = 0,
                    presence_penalty = 0
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, 
                    $"{_settings.Endpoint}/openai/deployments/{_settings.DeploymentName}/chat/completions?api-version={_settings.ApiVersion}")
                {
                    Content = requestContent
                };
                request.Headers.Add("api-key", _settings.ApiKey);

                _logger.LogInformation("Sending request to: {Url}", request.RequestUri);
                var response = await SendRequestWithRetryAsync(request);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Received response: {Response}", responseContent);
                
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var analysis = jsonResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "No analysis available";

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query with AI. Endpoint: {Endpoint}, Deployment: {Deployment}", 
                    _settings.Endpoint, _settings.DeploymentName);
                return $"Error analyzing query: {ex.Message}";
            }
        }

        private async Task<string> SendOpenAIRequestAsync(string prompt)
        {
            try
            {
                var requestUrl = $"{_settings.Endpoint}/openai/deployments/{_settings.DeploymentName}/completions?api-version={_settings.ApiVersion}";

                var requestData = new
                {
                    prompt = prompt,
                    max_tokens = _settings.MaxTokens,
                    temperature = _settings.Temperature,
                    top_p = 1,
                    frequency_penalty = 0,
                    presence_penalty = 0,
                    stop = (string[])null
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);

                var response = await _httpClient.PostAsync(requestUrl, requestContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (responseObject.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("text", out var text))
                {
                    return text.GetString()?.Trim() ?? "No response from AI service";
                }

                return "Invalid response format from AI service";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending request to OpenAI");
                return $"Error: {ex.Message}";
            }
        }

        private async Task<HttpResponseMessage> SendRequestWithRetryAsync(HttpRequestMessage request)
        {
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Attempt {Attempt} - Sending request to: {Url}", attempt + 1, request.RequestUri);
                    var response = await _httpClient.SendAsync(request);
                    
                    _logger.LogInformation("Attempt {Attempt} - Response status: {Status}", attempt + 1, response.StatusCode);
                    
                    if (response.IsSuccessStatusCode)
                        return response;
                        
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Attempt {Attempt} - Error response: {Response}", attempt + 1, responseContent);

                    // Parse the error response to get the actual wait time
                    int waitTimeSeconds = 60; // Default to 60 seconds for rate limits
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        try
                        {
                            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                            if (errorResponse.TryGetProperty("error", out var error) &&
                                error.TryGetProperty("message", out var message))
                            {
                                var msg = message.GetString();
                                if (msg != null)
                                {
                                    // Try to extract wait time from message
                                    var waitMatch = System.Text.RegularExpressions.Regex.Match(msg, @"retry after (\d+) seconds");
                                    if (waitMatch.Success && int.TryParse(waitMatch.Groups[1].Value, out int seconds))
                                    {
                                        waitTimeSeconds = seconds;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse error response for wait time");
                        }

                        if (attempt == MaxRetries)
                        {
                            throw new HttpRequestException($"Max retries exceeded for rate-limited request. Last response: {responseContent}");
                        }

                        _logger.LogWarning($"Rate limited by Azure OpenAI. Waiting {waitTimeSeconds} seconds before retry. Attempt {attempt + 1} of {MaxRetries}");
                        await Task.Delay(waitTimeSeconds * 1000);
                        request = await CloneHttpRequestMessageAsync(request);
                        continue;
                    }
                        
                    throw new HttpRequestException($"Request failed with status code {response.StatusCode}. Response: {responseContent}");
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning(ex, $"Request attempt {attempt + 1} failed. Retrying...");
                    await Task.Delay(InitialRetryDelayMs * (int)Math.Pow(2, attempt));
                    request = await CloneHttpRequestMessageAsync(request);
                }
            }
            
            throw new HttpRequestException("All retry attempts failed");
        }

        private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            
            if (request.Content != null)
            {
                var originalContent = await request.Content.ReadAsStringAsync();
                clone.Content = new StringContent(originalContent, System.Text.Encoding.UTF8, "application/json");
            }
            
            foreach (var header in request.Headers)
                clone.Headers.Add(header.Key, header.Value);
                
            return clone;
        }

        public async Task<QueryOptimizationResult> OptimizeQueryAsync(string query, string databaseContext)
        {
            try
            {
                _logger.LogInformation("Optimizing query with AI: {Query}", query);

                string model = _settings.ModelName ?? "gpt-4";

                var messages = new List<object>
                {
                    new { role = "system", content = "You are an expert SQL query optimizer. Analyze the provided SQL query and suggest optimizations." },
                    new { role = "user", content = $"Optimize this SQL query for a SQL Server database:\n\n{query}\n\nDatabase context: {databaseContext}" }
                };

                var requestData = new
                {
                    model = model,
                    messages = messages,
                    max_tokens = _settings.MaxTokens,
                    temperature = _settings.Temperature,
                    top_p = 1,
                    frequency_penalty = 0,
                    presence_penalty = 0
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, 
                    $"{_settings.Endpoint}/openai/deployments/{_settings.DeploymentName}/chat/completions?api-version={_settings.ApiVersion}")
                {
                    Content = requestContent
                };
                request.Headers.Add("api-key", _settings.ApiKey);

                var response = await SendRequestWithRetryAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                return ParseOptimizationResponse(responseContent, query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing query with AI");
                return new QueryOptimizationResult
                {
                    OptimizedQuery = query,
                    Explanation = $"Error optimizing query: {ex.Message}",
                    IndexRecommendations = new List<string>(),
                    IsSimulated = false
                };
            }
        }

        private QueryOptimizationResult ParseOptimizationResponse(string responseContent, string originalQuery)
        {
            try
            {
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

                string? optimizedQuery = null;
                string? explanation = null;
                var indexRecommendations = new List<string>();

                if (responseObject.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    var responseText = content.GetString();
                    if (responseText != null)
                    {
                        // Extract optimized query
                        optimizedQuery = ExtractOptimizedQuery(responseText) ?? originalQuery;
                        
                        // Extract explanation
                        explanation = ExtractExplanation(responseText) ?? "No explanation provided";
                        
                        // Extract index recommendations
                        indexRecommendations = ExtractIndexRecommendations(responseText);
                    }
                }

                return new QueryOptimizationResult
                {
                    OptimizedQuery = optimizedQuery ?? originalQuery,
                    Explanation = explanation ?? "Failed to parse AI response",
                    IndexRecommendations = indexRecommendations,
                    IsSimulated = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing optimization response");
                return new QueryOptimizationResult
                {
                    OptimizedQuery = originalQuery,
                    Explanation = $"Error parsing response: {ex.Message}",
                    IndexRecommendations = new List<string>(),
                    IsSimulated = false
                };
            }
        }

        private string? ExtractOptimizedQuery(string responseText)
        {
            // Simple extraction logic - look for SQL between ```sql and ``` markers
            var startMarker = "```sql";
            var endMarker = "```";
            
            var startIndex = responseText.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
            if (startIndex >= 0)
            {
                startIndex += startMarker.Length;
                var endIndex = responseText.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex >= 0)
                {
                    return responseText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            
            // Fallback: try without the sql marker
            startIndex = responseText.IndexOf("```");
            if (startIndex >= 0)
            {
                startIndex += 3;
                var endIndex = responseText.IndexOf("```", startIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex >= 0)
                {
                    return responseText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            
            return null;
        }

        private string? ExtractExplanation(string responseText)
        {
            // Look for explanation after the code block
            var marker = "```";
            var lastCodeBlockIndex = responseText.LastIndexOf(marker);
            
            if (lastCodeBlockIndex >= 0)
            {
                var explanationStart = lastCodeBlockIndex + marker.Length;
                if (explanationStart < responseText.Length)
                {
                    return responseText.Substring(explanationStart).Trim();
                }
            }
            
            // If no code block found, return the whole response
            return responseText;
        }

        private List<string> ExtractIndexRecommendations(string responseText)
        {
            var recommendations = new List<string>();
            
            // Look for index recommendations in the text
            // This is a simple implementation - could be enhanced with regex
            var indexMarkers = new[] { "CREATE INDEX", "CREATE NONCLUSTERED INDEX", "INDEX RECOMMENDATION" };
            
            var lines = responseText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                foreach (var marker in indexMarkers)
                {
                    if (line.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    {
                        recommendations.Add(line.Trim());
                        break;
                    }
                }
            }
            
            return recommendations;
        }
    }
} 