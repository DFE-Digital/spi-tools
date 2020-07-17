using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp
{
    public interface IUkrlpHistoricalRepository
    {
        Task<UkrlpDayData> GetDayDataAsync(DateTime date, CancellationToken cancellationToken);
    }
}