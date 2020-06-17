using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.HistoricalDataCapture.Application.Gias;
using Microsoft.Azure.WebJobs;

namespace Dfe.Spi.HistoricalDataCapture.Functions.Gias
{
    public class TimedDownloadGiasData
    {
        private const string FunctionName = nameof(TimedDownloadGiasData);
        private const string ScheduleExpression = "%SPI_Gias:DownloadSchedule%";

        private readonly IGiasDownloader _giasDownloader;
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly ILoggerWrapper _logger;

        public TimedDownloadGiasData(
            IGiasDownloader giasDownloader,
            IHttpSpiExecutionContextManager httpSpiExecutionContextManager, 
            ILoggerWrapper logger)
        {
            _giasDownloader = giasDownloader;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
            _logger = logger;
        }
        
        [FunctionName(FunctionName)]
        public async Task RunAsync([TimerTrigger(ScheduleExpression)] TimerInfo timerInfo, CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetInternalRequestId(Guid.NewGuid());

            _logger.Info($"{FunctionName} started at {DateTime.UtcNow}. Past due: {timerInfo.IsPastDue}");

            await _giasDownloader.DownloadAsync(cancellationToken);
        }
    }
}