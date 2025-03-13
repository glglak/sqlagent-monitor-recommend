using System.Data;
using System.Threading.Tasks;

namespace SqlMonitor.Interfaces
{
    public interface ISqlConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
        Task<IDbConnection> CreateConnectionAsync(string connectionString);
    }
} 