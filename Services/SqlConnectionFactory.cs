using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;
using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SqlMonitor.Services
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly SqlServerSettings _settings;
        private readonly ILogger<SqlConnectionFactory> _logger;

        public SqlConnectionFactory(IOptions<SqlServerSettings> settings, ILogger<SqlConnectionFactory> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<DbConnection> GetConnectionAsync(string databaseName)
        {
            return await CreateConnectionAsync(databaseName);
        }

        public string GetConnectionString(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("Database name cannot be null or empty");
            }

            var builder = new SqlConnectionStringBuilder(_settings.ConnectionString);
            
            if (!databaseName.Equals("master", StringComparison.OrdinalIgnoreCase))
            {
                var monitoredDb = _settings.MonitoredDatabases
                    .FirstOrDefault(db => db.Name.Equals(databaseName, StringComparison.OrdinalIgnoreCase));

                if (monitoredDb != null && !string.IsNullOrEmpty(monitoredDb.ConnectionString))
                {
                    return monitoredDb.ConnectionString;
                }

                builder.InitialCatalog = databaseName;
            }
            
            return builder.ConnectionString;
        }

        public async Task<DbConnection> CreateConnectionAsync(string? databaseName)
        {
            try
            {
                if (string.IsNullOrEmpty(databaseName))
                {
                    throw new ArgumentException("Database name cannot be null or empty");
                }

                // Get the base connection string from settings
                var connectionString = _settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException("Base connection string is not configured in settings");
                }

                // If this is a monitored database (not master), try to get its specific connection string
                if (!string.Equals(databaseName, "master", StringComparison.OrdinalIgnoreCase))
                {
                    var monitoredDb = _settings.MonitoredDatabases.FirstOrDefault(db => 
                        string.Equals(db.Name, databaseName, StringComparison.OrdinalIgnoreCase));
                    
                    if (monitoredDb != null)
                    {
                        if (!string.IsNullOrEmpty(monitoredDb.ConnectionString))
                        {
                            connectionString = monitoredDb.ConnectionString;
                            _logger.LogInformation($"Using specific connection string for database: {databaseName}");
                        }
                        else
                        {
                            // If no specific connection string found, modify the base connection string to use this database
                            var builder = new SqlConnectionStringBuilder(connectionString)
                            {
                                InitialCatalog = databaseName
                            };
                            connectionString = builder.ConnectionString;
                            _logger.LogInformation($"Using modified base connection string for database: {databaseName}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Database {databaseName} is not in the monitored databases list");
                        // Still proceed with connection attempt using base connection string
                        var builder = new SqlConnectionStringBuilder(connectionString)
                        {
                            InitialCatalog = databaseName
                        };
                        connectionString = builder.ConnectionString;
                    }
                }
                else
                {
                    _logger.LogInformation("Creating connection to master database");
                }

                // Validate the final connection string
                var finalBuilder = new SqlConnectionStringBuilder(connectionString);
                if (string.IsNullOrEmpty(finalBuilder.InitialCatalog))
                {
                    _logger.LogWarning("Connection string does not specify a database name");
                    finalBuilder.InitialCatalog = databaseName;
                    connectionString = finalBuilder.ConnectionString;
                }

                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating connection for database: {databaseName}");
                throw;
            }
        }

        private async Task<DbConnection> CreateConnectionWithStringAsync(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty");
            }

            try
            {
                _logger.LogInformation("Creating connection with provided connection string");
                
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating connection with provided connection string");
                throw;
            }
        }
    }
} 