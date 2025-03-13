using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlMonitor.Models;

namespace SqlMonitor.Interfaces
{
    public interface IIndexMonitorService
    {
        Task<IEnumerable<IndexInfo>> GetFragmentedIndexesAsync(CancellationToken cancellationToken);
        Task ReindexAsync(IndexInfo indexInfo, CancellationToken cancellationToken);
    }
} 