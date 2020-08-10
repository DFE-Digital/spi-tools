using CommandLine;

namespace Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp
{
    public class CommandLineOptions
    {
        [Option('d', "debug", Required = false, Default = false, HelpText = "Include debug logs")]
        public bool IncludeDebug { get; set; }
        
        [Option("log-directory", Required = false, HelpText = "Directory to log to. Default is application running directory")]
        public string LogDirectory { get; set; }
        
        [Option("data-directory", Required = false, HelpText = "Directory to store data in. Default is application running directory")]
        public string DataDirectory { get; set; }
        
        [Option("storage-connection-string", Required = false, HelpText = "Connection string to GIAS adapter storage account")]
        public string StorageConnectionString { get; set; }
    }
}