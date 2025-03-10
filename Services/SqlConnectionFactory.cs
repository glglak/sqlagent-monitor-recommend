using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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
            var connection = new SqlConnection(_settings.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
} 