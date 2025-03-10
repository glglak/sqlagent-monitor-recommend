using System.Data;
using System.Threading.Tasks;

namespace SqlMonitor.Services
{
    public interface ISqlConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
    }
} 