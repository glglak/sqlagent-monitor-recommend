# SQL Server Monitor and Recommend

A .NET 8 application that monitors SQL Server databases for performance issues, automatically reindexes fragmented indexes, and provides AI-powered recommendations for slow queries.

## Features

- **Automatic Index Monitoring**: Detects and fixes fragmented indexes
- **Slow Query Detection**: Identifies poorly performing queries
- **AI-Powered Analysis**: Uses Azure OpenAI or Claude to analyze and recommend optimizations for slow queries
- **Email Notifications**: Sends alerts for critical performance issues
- **REST API**: Provides endpoints for monitoring and management
- **Background Processing**: Runs monitoring tasks in the background

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- Azure OpenAI API key (or Claude API key) for AI-powered recommendations

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/sqlagent-monitor-recommend.git
   cd sqlagent-monitor-recommend
   ```

2. Install required packages:
   ```
   dotnet restore
   ```

3. Update the connection strings in `appsettings.Development.json` to point to your SQL Server instance.

4. Create the required databases:
   ```
   dotnet ef database update
   ```

5. Run the application:
   ```
   dotnet run --environment Development
   ```

### Configuration

The application is configured through `appsettings.json` and environment-specific files like `appsettings.Development.json`. Key configuration sections include:

#### SQL Server Settings

```json
"SqlServer": {
  "ConnectionString": "Server=YourServer;Database=SqlMonitor;Trusted_Connection=True;",
  "MonitoringIntervalMinutes": 30,
  "SlowQueryThresholdMs": 1000,
  "IndexFragmentationThreshold": 30,
  "MonitoredDatabases": [
    {
      "Name": "Database1",
      "ConnectionString": "Server=YourServer;Database=Database1;Trusted_Connection=True;"
    },
    {
      "Name": "Database2",
      "ConnectionString": "Server=YourServer;Database=Database2;Trusted_Connection=True;"
    }
  ]
}
```

#### AI Settings

```json
"AI": {
  "Provider": "AzureOpenAI", // or "Claude"
  "ApiKey": "your-api-key",
  "Endpoint": "https://your-resource-name.openai.azure.com/",
  "DeploymentName": "your-deployment-name",
  "ModelName": "gpt-4",
  "MaxTokens": 1000,
  "Temperature": 0.0
}
```

#### Notification Settings

```json
"Notifications": {
  "Email": {
    "Enabled": true,
    "SmtpServer": "smtp.office365.com",
    "Port": 587,
    "Username": "your-email@your-domain.com",
    "Password": "your-email-password",
    "FromAddress": "sql-monitor@your-domain.com",
    "ToAddresses": ["admin@your-domain.com"]
  },
  "SlowQueryThresholds": {
    "Critical": 5000,
    "Warning": 2000
  }
}
```

## API Endpoints

### Index Monitoring

- `GET /api/indexmonitor` - Get all fragmented indexes
- `POST /api/indexmonitor/{database}/{schema}/{table}/{index}/reindex` - Manually trigger reindexing

### Query Performance

- `GET /api/queryperformance/slow-queries/current` - Get current slow queries
- `GET /api/queryperformance/slow-queries/history?startDate={date}&endDate={date}` - Get historical slow queries
- `POST /api/queryperformance/analyze` - Analyze a specific query with AI

## Architecture

The application follows a clean architecture pattern with the following components:

- **Models**: Data structures representing SQL Server objects and monitoring data
- **Interfaces**: Abstractions for services
- **Services**: Core business logic for monitoring and optimization
- **Background Services**: Long-running tasks for periodic monitoring
- **Controllers**: API endpoints for user interaction
- **Data**: Database context for storing monitoring history

## How It Works

### Index Monitoring

1. The `IndexMonitorBackgroundService` runs at configured intervals
2. It uses `IndexMonitorService` to scan for fragmented indexes
3. When fragmentation exceeds the threshold, it automatically reindexes
4. All operations are logged and can be viewed via the API

### Slow Query Detection

1. The `QueryPerformanceBackgroundService` runs at configured intervals
2. It uses `QueryPerformanceService` to identify slow queries
3. Slow queries are analyzed using AI via `AIQueryAnalysisService`
4. Results are stored in the database and notifications are sent
5. Recommendations can be viewed via the API

## Security Considerations

- Connection strings and API keys should be stored securely
- Use Windows Authentication when possible for SQL Server connections
- Ensure proper access controls for the API endpoints
- Consider using Azure Key Vault or similar for production deployments

## Development

### Adding New Features

1. Create interfaces in the `Interfaces` folder
2. Implement services in the `Services` folder
3. Add API endpoints in the `Controllers` folder
4. Update dependency injection in `Program.cs`

### Testing

Run the included tests:

```dotnet test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Dapper](https://github.com/DapperLib/Dapper) - Used for database access
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - Used for ORM
- [Azure OpenAI](https://azure.microsoft.com/en-us/services/cognitive-services/openai-service/) - Used for AI-powered query analysis 