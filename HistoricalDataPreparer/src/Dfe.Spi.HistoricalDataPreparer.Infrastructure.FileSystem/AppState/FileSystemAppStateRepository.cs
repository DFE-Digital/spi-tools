using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.AppState;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.AppState
{
    public class FileSystemAppStateRepository : IAppStateRepository
    {
        private const string FileName = "last-processed-date.txt";

        private readonly string _filePath;
        private readonly DateTime _initialDate;

        public FileSystemAppStateRepository(string dataDirectory, DateTime initialDate)
        {
            _initialDate = initialDate;

            _filePath = Path.Combine(dataDirectory, FileName);
        }

        public async Task<DateTime> GetLastDateProcessedAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
            {
                return _initialDate;
            }

            await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            return DateTime.Parse(content);
        }

        public async Task SetLastDateProcessedAsync(DateTime lastDateProcessed, CancellationToken cancellationToken)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(_filePath));
            if (!directory.Exists)
            {
                directory.Create();
            }
            
            await using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            await using var writer = new StreamWriter(stream);

            await writer.WriteAsync(lastDateProcessed.ToString("O"));
            await writer.FlushAsync();
            writer.Close();
        }
    }
}