# SQL Server Performance Monitor

A .NET 8 API-first solution for monitoring and optimizing SQL Server databases.

## Features

- **Automatic Index Monitoring**: Detects fragmented indexes and performs automatic reindexing
- **Slow Query Detection**: Identifies slow-running queries across monitored databases
- **AI-Powered Query Analysis**: Uses Azure OpenAI or Claude to analyze and suggest optimizations for slow queries
- **Historical Performance Tracking**: Stores performance data for trend analysis
- **Notification System**: Alerts administrators about critical performance issues
- **RESTful API**: Provides endpoints for monitoring and management

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- Azure OpenAI or Claude API access (for AI-powered query analysis)

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/sqlagent-monitor-recommend.git
   cd sqlagent-monitor-recommend
   ```

2. Create a development configuration file:
   ```
   cp appsettings.Development.json appsettings.json
   ```

3. Update the connection strings and API keys in `appsettings.json`

4. Create the monitoring database:
   ```sql
   CREATE DATABASE SqlMonitor;
   ```

5. Apply Entity Framework migrations:
   ```
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

6. Run the application:
   ```
   dotnet run
   ```

### Configuration

The application is configured through `appsettings.json`. Key settings include:

#### SQL Server Settings
```json
"SqlServer": {
  "ConnectionString": "Server=your_server;Database=SqlMonitor;User Id=your_username;Password=your_password;",
  "MonitoringIntervalMinutes": 30,
  "SlowQueryThresholdMs": 1000,
  "IndexFragmentationThreshold": 30,
  "MonitoredDatabases": [
    {
      "Name": "Database1",
      "ConnectionString": "Server=your_server;Database=Database1;User Id=your_username;Password=your_password;"
    }
  ]
}
```

#### AI Settings
```json
"AI": {
  "Provider": "AzureOpenAI", // or "Claude"
  "ApiKey": "your-api-key",
  "Endpoint": "https://your-resource.openai.azure.com/",
  "DeploymentName": "gpt-4",
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
- `POST /api/{database}/{schema}/{table}/{index}/reindex` - Manually trigger reindexing

### Query Performance

- `GET /api/queryperformance/slow-queries/current` - Get current slow queries
- `GET /api/queryperformance/slow-queries/history` - Get historical slow queries
- `POST /api/queryperformance/analyze` - Analyze a specific query with AI

## Architecture

The solution follows a clean architecture approach:

- **Interfaces**: Contains all service contracts
- **Models**: Data models for the application
- **Services**: Implementation of the service interfaces
- **Controllers**: API endpoints
- **BackgroundServices**: Long-running monitoring tasks
- **Data**: Database context and migrations

## How It Works

### Index Monitoring

1. The `IndexMonitorBackgroundService` runs at configured intervals
2. It uses `IIndexMonitorService` to detect fragmented indexes
3. Indexes exceeding the fragmentation threshold are automatically reindexed
4. All operations are logged and stored in the database

### Slow Query Detection

1. The `QueryPerformanceBackgroundService` runs at configured intervals
2. It uses `IQueryPerformanceService` to detect slow queries
3. Slow queries are analyzed using the AI service
4. Results are stored in the database and notifications are sent if thresholds are exceeded

### AI Query Analysis

1. Slow queries are sent to Azure OpenAI or Claude
2. The AI analyzes the query and execution plan
3. It provides optimization suggestions
4. These suggestions are stored with the query history

## Development

### Adding a New Monitored Database

1. Add the database connection string to the `MonitoredDatabases` array in `appsettings.json`
2. Restart the application

### Extending the Solution

- **Custom Notifications**: Implement the `INotificationService` interface
- **Additional Metrics**: Add new properties to the models and update the services
- **UI Dashboard**: Build a frontend using the API endpoints

## Security Considerations

- Connection strings and API keys are stored in `appsettings.json`, which is excluded from source control
- Use environment-specific configuration files for different environments
- Consider using Azure Key Vault or similar services for production deployments
- Ensure the SQL Server user has appropriate permissions (read-only where possible)

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Dapper](https://github.com/DapperLib/Dapper) - Used for database access
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - Used for ORM
- [Azure OpenAI](https://azure.microsoft.com/en-us/services/cognitive-services/openai-service/) - Used for AI-powered query analysis 