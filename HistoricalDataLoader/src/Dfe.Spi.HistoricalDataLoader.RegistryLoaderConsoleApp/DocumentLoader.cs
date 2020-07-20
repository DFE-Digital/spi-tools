using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Serilog;

namespace Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp
{
    public class DocumentLoader
    {
        private readonly string _dataDirectory;
        private readonly ILogger _logger;
        private Container _container;

        public DocumentLoader(
            string dataDirectory,
            string cosmosUri,
            string cosmosKey,
            string cosmosDbName,
            string cosmosContainerName,
            ILogger logger)
        {
            _dataDirectory = dataDirectory;
            _logger = logger;
            var client = new CosmosClient(
                cosmosUri,
                cosmosKey,
                new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    },
                });
            _container = client.GetDatabase(cosmosDbName).GetContainer(cosmosContainerName);
        }

        internal async Task LoadAsync(CancellationToken cancellationToken)
        {
        }
    }
}