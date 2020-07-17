using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.AppState;
using Serilog;
using Serilog.Context;

namespace Dfe.Spi.HistoricalDataPreparer.Application
{
    public class HistoricalDataProcessor
    {
        private readonly IAppStateRepository _appStateRepository;
        private readonly ILogger _logger;

        public HistoricalDataProcessor(
            IAppStateRepository appStateRepository,
            ILogger logger)
        {
            _appStateRepository = appStateRepository;
            _logger = logger;
        }

        public async Task ProcessAvailableHistoricalDataAsync(CancellationToken cancellationToken)
        {
            var dateLastProcessed = await _appStateRepository.GetLastDateProcessedAsync(cancellationToken);
            var date = dateLastProcessed.Date.AddDays(1);
            _logger.Information("Starting at date {Date}", date.ToString("yyyy-MM-dd"));
            while (date < DateTime.Today && !cancellationToken.IsCancellationRequested)
            {
                using (LogContext.PushProperty("Date", date.ToString("yyyy-MM-dd")))
                {
                    _logger.Information($"Starting to process {date:yyyy-MM-dd}...");
                    
                    // TODO: Get data from GIAS
                    
                    // TODO: Get data from UKRLP

                    date = date.AddDays(1);
                    await _appStateRepository.SetLastDateProcessedAsync(date, cancellationToken);
                }
            }
        }
    }
}