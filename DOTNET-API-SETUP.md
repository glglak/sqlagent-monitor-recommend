# .NET 8 API Setup for SQL Performance Monitor

This document provides instructions on how to configure your .NET 8 API to work with the React frontend for the SQL Performance Monitor application.

## Required API Endpoints

Your .NET 8 API **MUST** implement the following endpoints exactly as specified:

- `GET /api/databases` - Get all databases
- `GET /api/databases/{databaseId}/status` - Check database status
- `GET /api/databases/{databaseId}/performance` - Get performance metrics
- `GET /api/databases/{databaseId}/slowqueries` - Get slow queries
- `GET /api/databases/{databaseId}/missingindexes` - Get missing indexes
- `POST /api/databases/{databaseId}/simulate/slowquery` - Simulate a slow query
- `POST /api/databases/{databaseId}/simulate/createindex` - Simulate index creation
- `POST /api/databases/{databaseId}/fix/query/{queryId}` - Apply fix to a slow query
- `POST /api/openai/optimize-query` - Optimize a query using Azure OpenAI

## API Response Format

The React frontend expects specific response formats for each endpoint:

### GET /api/databases

```json
[
  {
    "id": "5",
    "name": "EcommerceSample",
    "server": "YOUR-SERVER-NAME",
    "status": "online"
  },
  {
    "id": "6",
    "name": "HRSample",
    "server": "YOUR-SERVER-NAME",
    "status": "online"
  }
]
```

### GET /api/databases/{databaseId}/status

```json
{
  "status": "online"
}
```

### GET /api/databases/{databaseId}/performance

```json
{
  "timestamps": ["12:00:01", "12:00:02", "12:00:03", "12:00:04", "12:00:05"],
  "cpu": [25, 30, 28, 35, 40],
  "memory": [45, 48, 50, 52, 55],
  "diskIO": [10, 12, 8, 15, 11],
  "networkIO": [5, 8, 6, 9, 7]
}
```

## Controller Implementation

Here's a complete example of a controller that implements the required endpoints:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SqlPerformanceMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabasesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabasesController> _logger;
        private readonly string _connectionString;

        public DatabasesController(IConfiguration configuration, ILogger<DatabasesController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("SqlServer");
        }

        [HttpGet]
        public async Task<IActionResult> GetDatabases()
        {
            try
            {
                var databases = new List<object>();
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(@"
                        SELECT 
                            database_id as id,
                            name,
                            @@SERVERNAME as server,
                            state_desc as status
                        FROM sys.databases
                        WHERE database_id > 4  -- Skip system databases
                        ORDER BY name", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                databases.Add(new
                                {
                                    id = reader["id"].ToString(),
                                    name = reader["name"].ToString(),
                                    server = reader["server"].ToString(),
                                    status = reader["status"].ToString().ToLower() == "online" ? "online" : "offline"
                                });
                            }
                        }
                    }
                }
                
                return Ok(databases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting databases");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{databaseId}/status")]
        public async Task<IActionResult> GetDatabaseStatus(string databaseId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(@"
                        SELECT state_desc
                        FROM sys.databases
                        WHERE database_id = @databaseId", connection))
                    {
                        command.Parameters.AddWithValue("@databaseId", databaseId);
                        var status = (string)await command.ExecuteScalarAsync();
                        
                        return Ok(new { status = status?.ToLower() == "online" ? "online" : "offline" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting status for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{databaseId}/performance")]
        public async Task<IActionResult> GetPerformanceMetrics(string databaseId)
        {
            try
            {
                // Get the database name from the ID
                string dbName;
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT name FROM sys.databases WHERE database_id = @databaseId", connection))
                    {
                        command.Parameters.AddWithValue("@databaseId", databaseId);
                        dbName = (string)await command.ExecuteScalarAsync();
                    }
                }

                if (string.IsNullOrEmpty(dbName))
                {
                    return NotFound(new { error = "Database not found" });
                }

                // Get performance metrics
                var timestamps = new List<string>();
                var cpu = new List<double>();
                var memory = new List<double>();
                var diskIO = new List<double>();
                var networkIO = new List<double>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // CPU metrics
                    using (var command = new SqlCommand(@"
                        SELECT TOP(6) 
                            CONVERT(VARCHAR, DATEADD(ms, -1 * (ts_now - [timestamp]), GETDATE()), 108) AS time_stamp,
                            SQLProcessUtilization AS cpu_utilization
                        FROM (
                            SELECT 
                                record_id,
                                [timestamp],
                                CONVERT(int, record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int')) AS SQLProcessUtilization,
                                CONVERT(int, timestamp) AS ts_now
                            FROM (
                                SELECT
                                    timestamp,
                                    CONVERT(xml, record) AS record,
                                    ROW_NUMBER() OVER(ORDER BY timestamp) AS record_id
                                FROM sys.dm_os_ring_buffers
                                WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
                                AND record LIKE '%<SystemHealth>%'
                            ) AS x
                        ) AS y
                        ORDER BY record_id DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                timestamps.Add(reader["time_stamp"].ToString());
                                cpu.Add(Convert.ToDouble(reader["cpu_utilization"]));
                            }
                        }
                    }
                    
                    // Memory metrics
                    using (var command = new SqlCommand(@"
                        SELECT TOP(6)
                            CAST((physical_memory_in_use_kb * 1.0 / total_physical_memory_kb) * 100 AS DECIMAL(5,2)) AS memory_utilization
                        FROM sys.dm_os_process_memory
                        CROSS JOIN (
                            SELECT COUNT(*) AS cnt
                            FROM sys.dm_os_performance_counters
                            WHERE counter_name = 'Total Server Memory (KB)'
                        ) AS x
                        ORDER BY memory_utilization DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                memory.Add(Convert.ToDouble(reader["memory_utilization"]));
                            }
                        }
                    }
                    
                    // Disk I/O metrics
                    using (var command = new SqlCommand(@"
                        SELECT TOP(6)
                            CAST(AVG(io_stall_read_ms + io_stall_write_ms) / 
                                CASE WHEN AVG(num_of_reads + num_of_writes) = 0 THEN 1 
                                    ELSE AVG(num_of_reads + num_of_writes) END AS DECIMAL(10,2)) AS io_latency
                        FROM sys.dm_io_virtual_file_stats(DB_ID(@dbName), NULL)
                        GROUP BY DB_NAME(database_id)
                        ORDER BY io_latency DESC", connection))
                    {
                        command.Parameters.AddWithValue("@dbName", dbName);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                diskIO.Add(Convert.ToDouble(reader["io_latency"]));
                            }
                        }
                    }
                    
                    // Network I/O metrics
                    using (var command = new SqlCommand(@"
                        SELECT TOP(6)
                            CAST(SUM(bytes_sent + bytes_received) / (1024.0 * 1024.0) AS DECIMAL(10,2)) AS network_io_mb
                        FROM sys.dm_exec_connections
                        CROSS JOIN sys.dm_exec_sessions
                        WHERE sys.dm_exec_connections.session_id = sys.dm_exec_sessions.session_id
                        GROUP BY session_id
                        ORDER BY network_io_mb DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                networkIO.Add(Convert.ToDouble(reader["network_io_mb"]));
                            }
                        }
                    }
                }

                // Ensure we have data for all metrics
                while (memory.Count < timestamps.Count) memory.Add(0);
                while (diskIO.Count < timestamps.Count) diskIO.Add(0);
                while (networkIO.Count < timestamps.Count) networkIO.Add(0);

                // Return the performance metrics
                return Ok(new
                {
                    timestamps,
                    cpu,
                    memory,
                    diskIO,
                    networkIO
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting performance metrics for database {databaseId}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Implement other endpoints...
    }
}
```

## Program.cs Configuration

Make sure your `Program.cs` file is configured correctly:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "http://localhost:3003")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS before routing
app.UseCors("ReactApp");

app.UseRouting();
app.UseAuthorization();

// Map controllers with the correct route prefix
app.MapControllers();

// Ensure the app is listening on the correct port
app.Run();
```

## launchSettings.json Configuration

Make sure your `Properties/launchSettings.json` file is configured to use port 5000:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## appsettings.json Configuration

Make sure your `appsettings.json` file includes the SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=YOUR-SERVER-NAME;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com",
    "ApiKey": "your-azure-openai-api-key",
    "Deployment": "gpt-4",
    "ApiVersion": "2023-05-15"
  }
}
```

## Troubleshooting

If the React frontend cannot connect to your .NET 8 API:

1. **Check API is Running**: Make sure your .NET 8 API is running on port 5000
   ```
   dotnet run --urls="http://localhost:5000"
   ```

2. **Verify Endpoints**: Use Swagger UI (http://localhost:5000/swagger) to check if all endpoints are implemented correctly

3. **Check CORS**: Ensure CORS is properly configured to allow requests from your React app

4. **Test API Directly**: Use a tool like Postman to test the API endpoints directly

5. **Check Logs**: Look at the .NET API logs for any errors or exceptions

6. **Verify Controller Route**: Make sure your controller has the correct route attribute:
   ```csharp
   [Route("api/[controller]")]
   ```
   This will make the endpoint available at `/api/databases` (for a controller named `DatabasesController`)

7. **Check Port Conflicts**: Make sure no other application is using port 5000

8. **Firewall Settings**: Check if your firewall is blocking connections to port 5000 