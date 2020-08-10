using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Gias
{
    public interface IGiasHistoricalRepository
    {
        Task<GiasDayData> GetDayDataAsync(DateTime date, CancellationToken cancellationToken);
    }
}