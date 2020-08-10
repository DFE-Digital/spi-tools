using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Statistics;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Statistics
{
    public class FileSystemStatisticsRepository : IStatisticsRepository
    {
        private string _dataDirectory;

        public FileSystemStatisticsRepository(string dataDirectory)
        {
            _dataDirectory = Path.Combine(dataDirectory, "stats");
        }
        
        public async Task StoreDateStatisticsAsync(DateStatistics statistics, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(statistics);
            var fileName = $"{statistics.Date:yyyy-MM-dd}.json";

            await FileHelper.WriteStringToFileAsync(Path.Combine(_dataDirectory, fileName), json);
        }
    }
}