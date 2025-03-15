using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Interfaces
{
    public interface IIndexMonitorService
    {
        /// <summary>
        /// Gets fragmented indexes for all databases
        /// </summary>
        Task<IEnumerable<IndexInfo>> GetFragmentedIndexesAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Reindexes a fragmented index
        /// </summary>
        Task ReindexAsync(IndexInfo indexInfo, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets missing indexes for a database
        /// </summary>
        Task<List<MissingIndex>> GetMissingIndexesAsync(string databaseName);
        
        /// <summary>
        /// Simulates index creation on a database
        /// </summary>
        Task<IndexCreationResult> SimulateIndexCreationAsync(string databaseName, string table, string columns, string? includeColumns = null);

        /// <summary>
        /// Checks for fragmented indexes and logs them
        /// </summary>
        Task CheckFragmentedIndexesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks for missing indexes and logs them
        /// </summary>
        Task CheckMissingIndexesAsync(CancellationToken cancellationToken);
    }
} 