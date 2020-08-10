using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Statistics;

namespace Dfe.Spi.HistoricalDataPreparer.ConsoleApp
{
    public class InProcStatisticsRepository : IStatisticsRepository
    {
        private readonly IStatisticsRepository _persistentStore;
        private readonly List<DateStatistics> _statistics;

        public InProcStatisticsRepository(IStatisticsRepository persistentStore)
        {
            _persistentStore = persistentStore;
            _statistics = new List<DateStatistics>();
        }
        
        public async Task StoreDateStatisticsAsync(DateStatistics statistics, CancellationToken cancellationToken)
        {
            _statistics.Add(statistics);
            await _persistentStore.StoreDateStatisticsAsync(statistics, cancellationToken);
        }

        public IEnumerable<DateStatistics> Dates => _statistics;

        public int GetNumberOfDaysProcessed()
        {
            return _statistics.Count;
        }

        public TimeSpan GetTotalDuration()
        {
            var totalTicks = _statistics.Sum(s => s.Duration.Ticks);
            return new TimeSpan(totalTicks);
        }
    }
}