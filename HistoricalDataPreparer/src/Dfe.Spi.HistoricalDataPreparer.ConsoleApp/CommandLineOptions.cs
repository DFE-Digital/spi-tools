using CommandLine;

namespace Dfe.Spi.HistoricalDataPreparer.ConsoleApp
{
    public class CommandLineOptions
    {
        [Option('d', "debug", Required = false, Default = false, HelpText = "Include debug logs")]
        public bool IncludeDebug { get; set; }
        
        [Option("log-directory", Required = false, HelpText = "Directory to log to. Default is application running directory")]
        public string LogDirectory { get; set; }
        
        [Option("data-directory", Required = false, HelpText = "Directory to store data in. Default is application running directory")]
        public string DataDirectory { get; set; }
        
        [Option("historical-connection-string", Required = true, HelpText = "Connection string for Azure Storage account used by historical data download")]
        public string HistoricalConnectionString { get; set; }
        
        [Option("spi-translationapi-url", Required = true, HelpText = "Base url of SPI translation api")]
        public string SpiTranslationApiUrl { get; set; }
        
        [Option("spi-translationapi-subscription-key", Required = false, HelpText = "Subscription key of SPI translation api")]
        public string SpiTranslationApiSubscriptionKey { get; set; }
        
        [Option("spi-oauth-token-endpoint", Required = false, HelpText = "Token endpoint to perform authentication to")]
        public string SpiOAuthTokenEndpoint { get; set; }
        
        [Option("spi-oauth-client-id", Required = false, HelpText = "Client ID for authentication")]
        public string SpiOAuthClientId { get; set; }
        
        [Option("spi-oauth-client-secret", Required = false, HelpText = "Client secret for authentication")]
        public string SpiOAuthClientSecret { get; set; }
        
        [Option("spi-oauth-resource", Required = false, HelpText = "Resource for authentication")]
        public string SpiOAuthResource { get; set; }
    }
}