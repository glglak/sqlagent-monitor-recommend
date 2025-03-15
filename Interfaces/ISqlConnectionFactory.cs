using System.Data.Common;
using System.Threading.Tasks;

namespace SqlMonitor.Interfaces
{
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Creates a database connection to the specified database
        /// </summary>
        Task<DbConnection> GetConnectionAsync(string databaseName);
        
        /// <summary>
        /// Gets the connection string for the specified database
        /// </summary>
        string GetConnectionString(string databaseName);

        /// <summary>
        /// Creates a database connection using the provided connection string
        /// </summary>
        Task<DbConnection> CreateConnectionAsync(string? connectionString = null);
    }
} 