using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.HistoricalDataCapture.Domain.Configuration;
using Dfe.Spi.HistoricalDataCapture.Domain.Storage;
using Dfe.Spi.HistoricalDataCapture.Domain.UkrlpClient;

namespace Dfe.Spi.HistoricalDataCapture.Application.Ukrlp
{
    public interface IUkrlpDownloader
    {
        Task DownloadAsync(CancellationToken cancellationToken);
    }
    
    public class UkrlpDownloader : IUkrlpDownloader
    {
        private const string StorageFolderName = "ukrlp2";
        private const string LastChangeFileName = "lastchange.txt";
        
        private readonly IUkrlpClient _ukrlpClient;
        private readonly IStorage _storage;
        private readonly UkrlpConfiguration _configuration;
        private readonly ILoggerWrapper _logger;

        public UkrlpDownloader(
            IUkrlpClient ukrlpClient, 
            IStorage storage,
            UkrlpConfiguration configuration,
            ILoggerWrapper logger)
        {
            _ukrlpClient = ukrlpClient;
            _storage = storage;
            _configuration = configuration;
            _logger = logger;
        }
        
        public async Task DownloadAsync(CancellationToken cancellationToken)
        {
            // Get last pull
            var lastChange = await GetLastChangeTimeAsync(cancellationToken);
            var now = DateTime.UtcNow;
            
            // Download
            _logger.Info($"Starting to get UKRLP changes since {lastChange}...");
            var statuses = new[] {"A", "V", "PD1", "PD2"};
            foreach (var status in statuses)
            {
                var downloadBuffer = await _ukrlpClient.GetChangesSinceAsync(lastChange, status, cancellationToken);
                _logger.Info($"Ukrlp changes for status {status} downloaded, size: ${downloadBuffer.Length} bytes");
            
                // Store
                var fileName = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}Z-{status}.xml";
                _logger.Info($"Storing extract in folder {StorageFolderName} with name {fileName}...");
                await _storage.StoreAsync(StorageFolderName, fileName, downloadBuffer, cancellationToken);
                _logger.Info("Extract stored");
            }
            
            // Update last pull
            var updatedLastChangeBuffer = Encoding.UTF8.GetBytes(now.ToString());
            await _storage.StoreAsync(StorageFolderName, LastChangeFileName, updatedLastChangeBuffer, cancellationToken);
            _logger.Info($"Last change updated to {now}");
        }

        private async Task<DateTime> GetLastChangeTimeAsync(CancellationToken cancellationToken)
        {
            var data = await _storage.ReadAsync(StorageFolderName, LastChangeFileName, cancellationToken);
            if (data == null)
            {
                return DateTime.UtcNow.Date.AddDays(-14);
            }

            var dateString = Encoding.UTF8.GetString(data);
            var lastChange = DateTime.Parse(dateString);
            return lastChange;
        }
    }
}