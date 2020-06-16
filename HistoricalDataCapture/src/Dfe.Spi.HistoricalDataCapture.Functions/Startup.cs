using System.IO;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.HistoricalDataCapture.Application.Gias;
using Dfe.Spi.HistoricalDataCapture.Domain.Configuration;
using Dfe.Spi.HistoricalDataCapture.Domain.GiasClient;
using Dfe.Spi.HistoricalDataCapture.Domain.Storage;
using Dfe.Spi.HistoricalDataCapture.Functions;
using Dfe.Spi.HistoricalDataCapture.Infrastructure.AzureStorage;
using Dfe.Spi.HistoricalDataCapture.Infrastructure.GiasSoapApi;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestSharp;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Dfe.Spi.HistoricalDataCapture.Functions
{
    public class Startup : FunctionsStartup
    {
        
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var rawConfiguration = BuildConfiguration();
            Configure(builder, rawConfiguration);
        }

        public void Configure(IFunctionsHostBuilder builder, IConfigurationRoot rawConfiguration)
        {
            var services = builder.Services;

            AddConfiguration(services, rawConfiguration);
            AddLogging(services);
            AddHttp(services);
            AddGias(services);
            AddStorage(services);
        }

        private IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables(prefix: "SPI_")
                .Build();
        }
        
        private void AddConfiguration(IServiceCollection services, IConfigurationRoot rawConfiguration)
        {
            services.AddSingleton(rawConfiguration);
            
            var configuration = new HistoricalDataCaptureConfiguration();
            rawConfiguration.Bind(configuration);
            services.AddSingleton(configuration);
            services.AddSingleton(configuration.Gias);
            services.AddSingleton(configuration.Storage);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped<ILogger>(provider =>
                provider.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Registry")));
            
            services.AddScoped<IHttpSpiExecutionContextManager, HttpSpiExecutionContextManager>();
            services.AddScoped<ISpiExecutionContextManager>((provider) =>
                (ISpiExecutionContextManager) provider.GetService(typeof(IHttpSpiExecutionContextManager)));
            services.AddScoped<ILoggerWrapper, LoggerWrapper>();
        }

        private void AddHttp(IServiceCollection services)
        {
            services.AddTransient<IRestClient, RestClient>();
        }

        private void AddGias(IServiceCollection services)
        {
            services.AddScoped<IGiasDownloader, GiasDownloader>();
            services.AddScoped<IGiasClient, GiasSoapClient>();
        }

        private void AddStorage(IServiceCollection services)
        {
            services.AddScoped<IStorage, BlobStorageStorage>();
        }
    }
}