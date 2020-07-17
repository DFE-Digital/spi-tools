using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.HistoricalDataPreparer.Application;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.AppState;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Dfe.Spi.HistoricalDataPreparer.ConsoleApp
{
    class Program
    {
        static async Task RunAsync(CommandLineOptions options, ILogger logger, CancellationToken cancellationToken)
        {
            // Create processor + dependencies
            var appStateRepository = new FileSystemAppStateRepository(options.DataDirectory, new DateTime(2020, 06, 16));

            var processor = new HistoricalDataProcessor(
                appStateRepository,
                logger);

            // Run
            await processor.ProcessAvailableHistoricalDataAsync(cancellationToken);
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

            return new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: logMessageFormat)
                .WriteTo.File(logFileInfo.FullName, outputTemplate: logMessageFormat)
                .CreateLogger();
        }
    }
}