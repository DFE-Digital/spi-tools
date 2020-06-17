using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.HistoricalDataCapture.Domain.Configuration;
using Dfe.Spi.HistoricalDataCapture.Domain.GiasClient;
using Dfe.Spi.HistoricalDataCapture.Domain.Storage;

namespace Dfe.Spi.HistoricalDataCapture.Application.Gias
{
    public interface IGiasDownloader
    {
        Task DownloadAsync(CancellationToken cancellationToken);
    }
    
    public class GiasDownloader : IGiasDownloader
    {
        private readonly IGiasClient _giasClient;
        private readonly IStorage _storage;
        private readonly GiasConfiguration _configuration;
        private readonly ILoggerWrapper _logger;

        public GiasDownloader(
            IGiasClient giasClient, 
            IStorage storage,
            GiasConfiguration configuration,
            ILoggerWrapper logger)
        {
            _giasClient = giasClient;
            _storage = storage;
            _configuration = configuration;
            _logger = logger;
        }
        
        public async Task DownloadAsync(CancellationToken cancellationToken)
        {
            // Download
            _logger.Info("Starting to download GIAS extract...");
            var downloadBuffer = await _giasClient.DownloadExtractAsync(_configuration.ExtractId, cancellationToken);
            _logger.Info($"Gias extract downloaded, size: ${downloadBuffer.Length} bytes");
            
            // Store
            var folder = "gias";
            var fileName = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}Z.zip";
            _logger.Info($"Storing extract in folder {folder} with name {fileName}...");
            await _storage.StoreAsync(folder, fileName, downloadBuffer, cancellationToken);
            _logger.Info("Extract stored");
        }
    }
}