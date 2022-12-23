using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace Dfe.Spi.LocalPreparer.Azure.AzureStorage;

public interface IAzureStorageClientService
{
    Task<BlobContainerClient> GetBlobContainerClient(string containerName, bool remote = false);
    Task<string?> GetAzureStorageKeyAsync();
    Task<bool> CheckConnections();
    Task<QueueClient> GetQueueClient(string queueName, bool remote = false);
}