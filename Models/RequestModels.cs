namespace SqlMonitor.Models
{
    /// <summary>
    /// Request model for query analysis
    /// </summary>
    public class QueryAnalysisRequest
    {
        public string Query { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for query fix
    /// </summary>
    public class QueryFixRequest
    {
        public string Query { get; set; } = string.Empty;
    }
} 