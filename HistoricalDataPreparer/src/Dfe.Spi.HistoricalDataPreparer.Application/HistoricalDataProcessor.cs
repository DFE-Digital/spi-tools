using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.AppState;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;
using Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp;
using Serilog;
using Serilog.Context;

namespace Dfe.Spi.HistoricalDataPreparer.Application
{
    public class HistoricalDataProcessor
    {
        private readonly IAppStateRepository _appStateRepository;
        private readonly IGiasHistoricalRepository _giasHistoricalRepository;
        private readonly IUkrlpHistoricalRepository _ukrlpHistoricalRepository;
        private readonly IDayProcessor _dayProcessor;
        private readonly ILogger _logger;

        public HistoricalDataProcessor(
            IAppStateRepository appStateRepository,
            IGiasHistoricalRepository giasHistoricalRepository,
            IUkrlpHistoricalRepository ukrlpHistoricalRepository,
            IDayProcessor dayProcessor,
            ILogger logger)
        {
            _appStateRepository = appStateRepository;
            _giasHistoricalRepository = giasHistoricalRepository;
            _ukrlpHistoricalRepository = ukrlpHistoricalRepository;
            _dayProcessor = dayProcessor;
            _logger = logger;
        }

        public async Task ProcessAvailableHistoricalDataAsync(DateTime? maxDate, CancellationToken cancellationToken)
        {
            var endDate = maxDate ?? DateTime.Today;
            _logger.Information($"Processing historical data upto {endDate}");
            
            var dateLastProcessed = await _appStateRepository.GetLastDateProcessedAsync(cancellationToken);
            var date = dateLastProcessed.Date.AddDays(1);
            _logger.Information("Starting at date {Date}", date.ToString("yyyy-MM-dd"));
            while (date <= endDate && !cancellationToken.IsCancellationRequested)
            {
                using (LogContext.PushProperty("Date", date.ToString("yyyy-MM-dd")))
                {
                    _logger.Information($"Starting to process {date:yyyy-MM-dd}...");

                    var giasData = await ReadGiasData(date, cancellationToken);
                    var ukrlpData = await ReadUkrlpData(date, cancellationToken);

                    await _dayProcessor.ProcessDaysDataAsync(date, giasData, ukrlpData, cancellationToken);

                    await _appStateRepository.SetLastDateProcessedAsync(date, cancellationToken);
                    date = date.AddDays(1);
                }
            }
        }

        private async Task<GiasDayData> ReadGiasData(DateTime date, CancellationToken cancellationToken)
        {
            _logger.Information("Reading data from GIAS...");
            var giasData = await _giasHistoricalRepository.GetDayDataAsync(date, cancellationToken);
            _logger.Information("Read {NumberOfEstablishments} establishments, {NumberOfGroups} groups, {NumberOfLocalAuthorities} local authorities " +
                                "and {NumberOfGroupLinks} group links from GIAS", 
                giasData.Establishments.Length, giasData.Groups.Length, giasData.LocalAuthorities.Length, giasData.GroupLinks.Length);
            return giasData;
        }
        private async Task<UkrlpDayData> ReadUkrlpData(DateTime date, CancellationToken cancellationToken)
        {
            _logger.Information("Reading data from UKRLP...");
            var ukrlpData = await _ukrlpHistoricalRepository.GetDayDataAsync(date, cancellationToken);
            _logger.Information("Read {NumberOfProviders} providers from UKRLP", 
                ukrlpData.Providers.Length);
            return ukrlpData;
        }
    }
}