namespace SqlMonitor.Models
{
    public class AISettings
    {
        public string Provider { get; set; } = "AzureOpenAI"; // or "Claude"
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.0;
    }
} 