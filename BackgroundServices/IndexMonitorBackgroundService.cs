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
    public class IndexMonitorBackgroundService : BackgroundService
    {
        private readonly ILogger<IndexMonitorBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SqlServerSettings _settings;

        public IndexMonitorBackgroundService(
            ILogger<IndexMonitorBackgroundService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<SqlServerSettings> settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Index Monitor Background Service is starting");

            // Default to 15 minutes if not specified
            var interval = _settings.MonitoringIntervalMinutes > 0 
                ? _settings.MonitoringIntervalMinutes 
                : 15;

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Index Monitor Background Service is running");

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var indexMonitorService = scope.ServiceProvider.GetRequiredService<IIndexMonitorService>();
                        
                        // Check for fragmented indexes
                        var fragmentedIndexes = await indexMonitorService.GetFragmentedIndexesAsync(stoppingToken);
                        var fragmentedIndexesList = fragmentedIndexes.ToList();

                        if (fragmentedIndexesList.Any())
                        {
                            _logger.LogWarning($"Found {fragmentedIndexesList.Count} fragmented indexes");

                            // Group by database for logging
                            var indexesByDatabase = fragmentedIndexesList
                                .GroupBy(i => i.DatabaseName)
                                .ToDictionary(g => g.Key, g => g.ToList());

                            foreach (var dbGroup in indexesByDatabase)
                            {
                                // Log the fragmented indexes instead of sending notifications
                                _logger.LogWarning($"Found {dbGroup.Value.Count} fragmented indexes in database {dbGroup.Key}");

                                // Reindex if threshold is exceeded
                                foreach (var index in dbGroup.Value.Where(i => 
                                    i.FragmentationPercent >= _settings.IndexFragmentationThreshold && 
                                    i.PageCount > 100))
                                {
                                    _logger.LogInformation($"Reindexing {index.IndexName} on {index.TableName} in {index.DatabaseName}");
                                    await indexMonitorService.ReindexAsync(index, stoppingToken);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No fragmented indexes found");
                        }

                        // Check for missing indexes in each monitored database
                        foreach (var db in _settings.MonitoredDatabases)
                        {
                            var missingIndexes = await indexMonitorService.GetMissingIndexesAsync(db.Name);
                            
                            if (missingIndexes.Any())
                            {
                                _logger.LogWarning($"Found {missingIndexes.Count} missing indexes in {db.Name}");
                                
                                // Log significant missing indexes instead of sending notifications
                                var significantMissingIndexes = missingIndexes
                                    .Where(i => i.ImprovementPercent >= 50)
                                    .ToList();
                                    
                                if (significantMissingIndexes.Any())
                                {
                                    _logger.LogWarning($"Found {significantMissingIndexes.Count} significant missing indexes in {db.Name}");
                                    foreach (var index in significantMissingIndexes)
                                    {
                                        _logger.LogInformation($"Missing index on {index.Table}, columns: {index.Columns}, improvement: {index.ImprovementPercent}%");
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"No missing indexes found in {db.Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Index Monitor Background Service");
                }

                // Wait for the next interval
                _logger.LogInformation($"Index Monitor Background Service is sleeping for {interval} minutes");
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }

            _logger.LogInformation("Index Monitor Background Service is stopping");
        }
    }
} 