using CommandLine;

namespace Dfe.Spi.HistoricalDataPreparer.ConsoleApp
{
    public class CommandLineOptions
    {
        [Option("log-directory", Required = false, HelpText = "Directory to log to. Default is application running directory")]
        public string LogDirectory { get; set; }
        
        [Option("data-directory", Required = false, HelpText = "Directory to store data in. Default is application running directory")]
        public string DataDirectory { get; set; }
        
        [Option("historical-connection-string", Required = true, HelpText = "Connection string for Azure Storage account used by historical data download")]
        public string HistoricalConnectionString { get; set; }
    }
}