using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Statistics
{
    public interface IStatisticsRepository
    {
        Task StoreDateStatisticsAsync(DateStatistics statistics, CancellationToken cancellationToken);
    }
}