using CommandLine;

namespace Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp
{
    public class CommandLineOptions
    {
        [Option('d', "debug", Required = false, Default = false, HelpText = "Include debug logs")]
        public bool IncludeDebug { get; set; }
        
        [Option("log-directory", Required = false, HelpText = "Directory to log to. Default is application running directory")]
        public string LogDirectory { get; set; }
        
        [Option("data-directory", Required = true, HelpText = "Directory to store data in. Default is application running directory")]
        public string DataDirectory { get; set; }
        
        [Option("cosmos-uri", Required = true, HelpText = "URI of Cosmos instance")]
        public string CosmosUri { get; set; }
        
        [Option("cosmos-key", Required = true, HelpText = "Auth key of Cosmos instance")]
        public string CosmosKey { get; set; }
        
        [Option("cosmos-database", Required = true, HelpText = "Database name in the Cosmos instance")]
        public string CosmosDatabaseName { get; set; }
        
        [Option("cosmos-container", Required = true, HelpText = "Container name in the Cosmos instance")]
        public string CosmosContainerName { get; set; }
    }
}