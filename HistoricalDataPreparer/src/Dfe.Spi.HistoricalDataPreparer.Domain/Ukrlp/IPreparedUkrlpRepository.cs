using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp
{
    public interface IPreparedUkrlpRepository
    {
        Task<Provider> GetProviderAsync(long ukprn, DateTime date, CancellationToken cancellationToken);
        Task StoreProviderAsync(Provider provider, DateTime date, CancellationToken cancellationToken);
        Task FlushAsync();
    }
}