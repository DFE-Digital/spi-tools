using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.HistoricalDataPreparer.Application;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage.Gias;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage.Ukrlp;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.AppState;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Gias;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Statistics;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Ukrlp;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Dfe.Spi.HistoricalDataPreparer.ConsoleApp
{
    class Program
    {
        static async Task RunAsync(CommandLineOptions options, ILogger logger, CancellationToken cancellationToken)
        {
            // Create processor + dependencies
            var appStateRepository = new FileSystemAppStateRepository(options.DataDirectory, new DateTime(2020, 06, 16));
            var giasHistoricalRepository = new BlobGiasHistoricalRepository(options.HistoricalConnectionString);
            var ukrlpHistoricalRepository = new BlobUkrlpHistoricalRepository(options.HistoricalConnectionString);
            var preparedGiasRepository = new FileSystemPreparedGiasRepository(options.DataDirectory);
            var preparedUkrlpRepository = new FileSystemPreparedUkrlpRepository(options.DataDirectory);
            var statisticsRepository = new InProcStatisticsRepository(new FileSystemStatisticsRepository(options.DataDirectory));
            
            var dayProcessor = new DayProcessor(
                preparedGiasRepository, 
                preparedUkrlpRepository,
                statisticsRepository,
                logger);

            var processor = new HistoricalDataProcessor(
                appStateRepository,
                giasHistoricalRepository,
                ukrlpHistoricalRepository,
                dayProcessor,
                logger);
            
            // Initialise state
            await preparedGiasRepository.InitAsync(cancellationToken);
            await preparedUkrlpRepository.InitAsync(cancellationToken);

            // Run
            await processor.ProcessAvailableHistoricalDataAsync(cancellationToken);
            
            // Log stats
            logger.Information("Processed {NumberOfDays} days in {TotalDuration}", 
                statisticsRepository.GetNumberOfDaysProcessed(), statisticsRepository.GetTotalDuration());
            foreach (var dateStatistics in statisticsRepository.Dates)
            {
                logger.Information("Completed {Date} in {Duration}, which had {EstablishmentsChanged} establishments, {GroupsChanged} groups, " +
                                   "{LocalAuthoritiesChanged} local authorities and {ProvidersChanged} providers change",
                    dateStatistics.Date.ToString("yyyy-MM-dd"), dateStatistics.Duration, dateStatistics.EstablishmentsChanged, dateStatistics.GroupsChanged, 
                    dateStatistics.LocalAuthoritiesChanged, dateStatistics.ProvidersChanged);
            }
        }

        static void Main(string[] args)
        {
            CommandLineOptions options = null;
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsed) => options = parsed);
            if (options != null)
            {
                var logger = GetLogger(options);

                try
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var runTask = RunAsync(options, logger, cancellationTokenSource.Token);

                    while (true)
                    {
                        if (runTask.IsCompleted)
                        {
                            break;
                        }

                        while (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(false);
                            if (key.Key == ConsoleKey.Escape)
                            {
                                logger.Information("Shutting down...");
                                cancellationTokenSource.Cancel();
                                break;
                            }
                        }
                    }

                    runTask.Wait();
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException aex && aex.InnerExceptions.First() is TaskCanceledException)
                    {
                        return;
                    }
                    logger.Error(ex, "Unexpected error occured while preparing historical data");
                }
                finally
                {
                    logger.Information("All done");
                }
            }
        }

        static ILogger GetLogger(CommandLineOptions options)
        {
            var logDirectory = string.IsNullOrEmpty(options.LogDirectory)
                ? Environment.CurrentDirectory
                : options.LogDirectory;
            var fileName = $"log-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
            var path = Path.Combine(logDirectory, fileName);

            var logFileInfo = new FileInfo(path);
            Console.WriteLine($"Logging to {logFileInfo.FullName}");

            var logMessageFormat = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}";
            var level = options.IncludeDebug ? LogEventLevel.Debug : LogEventLevel.Information;

            return new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: logMessageFormat)
                .WriteTo.File(logFileInfo.FullName, outputTemplate: logMessageFormat)
                .CreateLogger();
        }
    }
}