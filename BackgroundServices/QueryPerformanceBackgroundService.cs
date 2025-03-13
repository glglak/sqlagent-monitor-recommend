using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

namespace SqlMonitor.BackgroundServices
{
    public class QueryPerformanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueryPerformanceBackgroundService> _logger;
        private readonly SqlServerSettings _settings;

        public QueryPerformanceBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<SqlServerSettings> settings,
            ILogger<QueryPerformanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Query Performance Monitor Background Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running slow query detection");

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var queryPerformanceService = scope.ServiceProvider.GetRequiredService<IQueryPerformanceService>();
                    
                    var slowQueries = await queryPerformanceService.GetSlowQueriesAsync(stoppingToken);
                    
                    _logger.LogInformation($"Found {slowQueries.Count()} slow queries");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while detecting slow queries");
                }

                await Task.Delay(TimeSpan.FromMinutes(_settings.MonitoringIntervalMinutes), stoppingToken);
            }
        }
    }
} 