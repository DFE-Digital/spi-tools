using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.AppState
{
    public interface IAppStateRepository
    {
        Task<DateTime> GetLastDateProcessedAsync(CancellationToken cancellationToken);
        Task SetLastDateProcessedAsync(DateTime lastDateProcessed, CancellationToken cancellationToken);
    }
}