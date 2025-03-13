using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SqlMonitor.Interfaces;
using SqlMonitor.Models;

namespace SqlMonitor.Services
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly SqlServerSettings _settings;

        public SqlConnectionFactory(IOptions<SqlServerSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            return await CreateConnectionAsync(_settings.ConnectionString);
        }

        public async Task<IDbConnection> CreateConnectionAsync(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
} 