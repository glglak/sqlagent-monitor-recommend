using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SqlMonitor.BackgroundServices
{
    public class QueryPerformanceBackgroundService : BackgroundService
    {
        private readonly ILogger<QueryPerformanceBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SqlServerSettings _settings;

        public QueryPerformanceBackgroundService(
            ILogger<QueryPerformanceBackgroundService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<SqlServerSettings> settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Query Performance Background Service is starting");

            // Default to 15 minutes if not specified
            var interval = _settings.MonitoringIntervalMinutes > 0 
                ? _settings.MonitoringIntervalMinutes 
                : 15;

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Query Performance Background Service is running");

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var queryPerformanceService = scope.ServiceProvider.GetRequiredService<IQueryPerformanceService>();
                        
                        // Check for slow queries
                        var slowQueries = await queryPerformanceService.GetSlowQueriesFromQueryStoreAsync(stoppingToken);
                        var slowQueriesList = slowQueries.ToList();

                        if (slowQueriesList.Any())
                        {
                            _logger.LogWarning($"Found {slowQueriesList.Count} slow queries");

                            // Get critical slow queries
                            var criticalSlowQueries = slowQueriesList
                                .Where(q => q.ExecutionTime * 1000 >= _settings.Notifications.SlowQueryThresholds.Critical)
                                .ToList();

                            if (criticalSlowQueries.Any())
                            {
                                _logger.LogCritical($"Found {criticalSlowQueries.Count} critical slow queries");

                                // Log critical slow queries instead of sending notifications
                                foreach (var query in criticalSlowQueries)
                                {
                                    // Determine the database name from the query context
                                    var databaseName = query.DatabaseName ?? "Unknown";
                                    _logger.LogCritical($"Critical slow query in {databaseName}: Execution time: {query.ExecutionTime}s, Execution count: {query.ExecutionCount}");
                                }
                            }

                            // Get warning slow queries
                            var warningSlowQueries = slowQueriesList
                                .Where(q => 
                                    q.ExecutionTime * 1000 >= _settings.Notifications.SlowQueryThresholds.Warning && 
                                    q.ExecutionTime * 1000 < _settings.Notifications.SlowQueryThresholds.Critical)
                                .ToList();

                            if (warningSlowQueries.Any())
                            {
                                _logger.LogWarning($"Found {warningSlowQueries.Count} warning slow queries");
                                
                                // Log warning slow queries
                                foreach (var query in warningSlowQueries.Take(3)) // Log only the top 3 to avoid flooding
                                {
                                    var databaseName = query.DatabaseName ?? "Unknown";
                                    _logger.LogWarning($"Warning slow query in {databaseName}: Execution time: {query.ExecutionTime}s, Execution count: {query.ExecutionCount}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No slow queries found");
                        }

                        // Check for slow queries in each monitored database
                        foreach (var db in _settings.MonitoredDatabases)
                        {
                            var dbSlowQueries = await queryPerformanceService.GetSlowQueriesAsync(db.Name);
                            
                            if (dbSlowQueries.Any())
                            {
                                _logger.LogWarning($"Found {dbSlowQueries.Count} slow queries in {db.Name}");
                                
                                // Log the slowest query instead of sending notifications
                                var slowestQuery = dbSlowQueries.OrderByDescending(q => q.ExecutionTime).First();
                                _logger.LogWarning($"Slowest query in {db.Name}: Execution time: {slowestQuery.ExecutionTime}s, CPU time: {slowestQuery.CpuTime}s, Logical reads: {slowestQuery.LogicalReads}");
                            }
                            else
                            {
                                _logger.LogInformation($"No slow queries found in {db.Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Query Performance Background Service");
                }

                // Wait for the next interval
                _logger.LogInformation($"Query Performance Background Service is sleeping for {interval} minutes");
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }

            _logger.LogInformation("Query Performance Background Service is stopping");
        }
    }
} 