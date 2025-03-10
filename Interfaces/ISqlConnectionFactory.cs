namespace SqlMonitor.Interfaces
{
    public interface ISqlConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
    }
} 