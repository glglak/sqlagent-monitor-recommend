namespace SqlMonitor.Models
{
    public class AISettings
    {
        public string Provider { get; set; } = "AzureOpenAI";
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 8000;
        public double Temperature { get; set; } = 0.0;
        public string ApiVersion { get; set; } = "2023-05-15"; // Default API version for Azure OpenAI
    }
} 