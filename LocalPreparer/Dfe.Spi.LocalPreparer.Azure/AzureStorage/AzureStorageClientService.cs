using Azure.Data.Tables;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure.AzureStorage
{
    public class AzureStorageClientService : IAzureStorageClientService
    {

        private readonly IOptions<SpiSettings> _configuration;
        private readonly IContextManager _contextManager;
        private readonly ServiceName _serviceName; 

        public AzureStorageClientService(IOptions<SpiSettings> configuration, IContextManager contextManager)
        {
            _configuration = configuration;
            _contextManager = contextManager;
            _serviceName = _contextManager.Context.ActiveService;
        }

        public async Task<BlobContainerClient> GetBlobContainerClient(string containerName, bool remote = false) =>
            new BlobContainerClient(await CreateConnectionStringAsync(remote), containerName);

        public async Task<QueueClient> GetQueueClient(string queueName, bool remote = false) =>
            new QueueClient(await CreateConnectionStringAsync(remote), queueName);

        public async Task<TableClient> GetTableClient(string tableName, bool remote = false) =>
            new TableClient(await CreateConnectionStringAsync(remote), tableName);


        private async Task<string> CreateConnectionStringAsync(bool remote = false)
        {
            if (!remote)
                return
                    $"AccountName={_configuration.Value.Services?.GetValueOrDefault(_serviceName)?.LocalStorageAccountName};" +
                    $"AccountKey={_configuration.Value.Services?.GetValueOrDefault(_serviceName)?.LocalStorageAccessKey};" +
                $"DefaultEndpointsProtocol=http;" +
                    $"BlobEndpoint={_configuration.Value.Services?.GetValueOrDefault(_serviceName)?.LocalStorageBlobEndpoint};" +
                    $"QueueEndpoint={_configuration.Value.Services?.GetValueOrDefault(_serviceName)?.LocalStorageQueueEndpoint};" +
                    $"TableEndpoint={_configuration.Value.Services?.GetValueOrDefault(_serviceName)?.LocalStorageTableEndpoint};";

            return $"DefaultEndpointsProtocol=https;AccountName={_configuration.Value.Services?.GetValueOrDefault(_serviceName)?.RemoteStorageAccountName};AccountKey={await GetAzureStorageKeyAsync()};EndpointSuffix=core.windows.net";

        }


        public async Task<string?> GetAzureStorageKeyAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_configuration.Value.Services?.GetValueOrDefault(_serviceName)?.RemoteStorageAccessKey))
                {
                    return _configuration.Value.Services?.GetValueOrDefault(_serviceName)?.RemoteStorageAccountName;
                }
                else
                {

                    if (_contextManager.Context.StorageAccountKeys.TryGetValue(_serviceName,
                            out string cachedKey)) return cachedKey;

                    Interactions.WriteColourLine(
                        $"{Environment.NewLine}Retrieving Storage Account details for {_serviceName}...{Environment.NewLine}",
                        ConsoleColor.Blue);

                    var accountName = _configuration.Value.Services?.GetValueOrDefault(_serviceName)
                        ?.RemoteStorageAccountName;
                    var resourceGroupName = accountName?.ToResourceGroup(_configuration.Value.Azure.AzureEnvironmentPrefix);
                    var subscription = _contextManager.Context.SubscriptionResource;
                    var resourceGroups = subscription.GetResourceGroups();
                    ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(resourceGroupName);
                    var storageAccount = await resourceGroup.GetStorageAccounts().GetAsync(accountName);
                    var key1 = storageAccount.Value.GetKeys().FirstOrDefault(x => x.KeyName == "key1");
                    if (key1 == null)
                        throw new Exception();

                    _contextManager.Context.StorageAccountKeys.Add(_serviceName, key1.Value);
                    return key1.Value;
                }
            }
            catch (Exception e)
            {
                throw new SpiException(new List<string>() { "Failed to retrieve Storage Account Access Key!" }, e);
            }

        }


        public async Task<bool> CheckConnections()
        {
            var errors = new List<string>();
            try
            {
                var blobContainerClient = await GetBlobContainerClient("test");
                await blobContainerClient.ExistsAsync();
            }
            catch
            {
                errors.Add("Connection to the Local Azure Storage failed. Please check your Access Key or connection string!");
            }

            try
            {
                var blobContainerClient = await GetBlobContainerClient("test", true);
                await blobContainerClient.ExistsAsync();
            }
            catch
            {
                errors.Add("Connection to the Remote Azure Storage failed. Please check your Access Key or connection string!");
            }
            if (!errors.Any())
                return true;
            throw new SpiException(errors, null);

        }
    }
}
