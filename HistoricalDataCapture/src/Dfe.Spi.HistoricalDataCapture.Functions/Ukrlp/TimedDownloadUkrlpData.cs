using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.HistoricalDataCapture.Application.Ukrlp;
using Microsoft.Azure.WebJobs;

namespace Dfe.Spi.HistoricalDataCapture.Functions.Ukrlp
{
    public class TimedDownloadUkrlpData
    {
        private const string FunctionName = nameof(TimedDownloadUkrlpData);
        private const string ScheduleExpression = "%SPI_Ukrlp:DownloadSchedule%";

        private readonly IUkrlpDownloader _ukrlpDownloader;
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly ILoggerWrapper _logger;

        public TimedDownloadUkrlpData(
            IUkrlpDownloader ukrlpDownloader,
            IHttpSpiExecutionContextManager httpSpiExecutionContextManager, 
            ILoggerWrapper logger)
        {
            _ukrlpDownloader = ukrlpDownloader;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
            _logger = logger;
        }
        
        [FunctionName(FunctionName)]
        public async Task RunAsync([TimerTrigger(ScheduleExpression)] TimerInfo timerInfo, CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetInternalRequestId(Guid.NewGuid());

            _logger.Info($"{FunctionName} started at {DateTime.UtcNow}. Past due: {timerInfo.IsPastDue}");

            await _ukrlpDownloader.DownloadAsync(cancellationToken);
        }
    }
}