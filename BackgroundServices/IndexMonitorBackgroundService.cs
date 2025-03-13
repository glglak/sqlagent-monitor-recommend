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
    public class IndexMonitorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IndexMonitorBackgroundService> _logger;
        private readonly SqlServerSettings _settings;

        public IndexMonitorBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<SqlServerSettings> settings,
            ILogger<IndexMonitorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Index Monitor Background Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running index fragmentation check");

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var indexMonitorService = scope.ServiceProvider.GetRequiredService<IIndexMonitorService>();
                    
                    var fragmentedIndexes = await indexMonitorService.GetFragmentedIndexesAsync(stoppingToken);
                    
                    foreach (var index in fragmentedIndexes)
                    {
                        _logger.LogInformation($"Found fragmented index: {index.DatabaseName}.{index.SchemaName}.{index.TableName}.{index.IndexName} - {index.FragmentationPercentage:F2}% fragmentation");
                        
                        if (index.NeedsReindexing)
                        {
                            await indexMonitorService.ReindexAsync(index, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking index fragmentation");
                }

                await Task.Delay(TimeSpan.FromMinutes(_settings.MonitoringIntervalMinutes), stoppingToken);
            }
        }
    }
} 