using Dfe.Spi.LocalPreparer.Domain.Enums;

namespace Dfe.Spi.LocalPreparer.Domain.Models;

public class SpiSettings
{
    public Dictionary<ServiceName, ServiceSettings>? Services { get; set; }
    public AzureSettings Azure { get; set; }
}

public class ServiceSettings
{
    public string? RemoteStorageAccountName { get; set; }
    public string? LocalStorageAccountName { get; set; }
    public string[]? Tables { get; set; }
    public string[]? TablesWithoutContent { get; set; }
    public string[]? Queues { get; set; }
    public string[]? BlobContainers { get; set; }
    public string[]? BlobContainersWithoutContent { get; set; }
    public string? RemoteStorageAccessKey { get; set; }
    public string? LocalStorageAccessKey { get; set; }
    public Uri? LocalStorageBlobEndpoint { get; set; }
    public Uri? LocalStorageTableEndpoint { get; set; }
    public Uri? LocalStorageQueueEndpoint { get; set; }
    public Uri? LocalCosmosEndpoint { get; set; }   
    public string? RemoteCosmosAccountName { get; set; }
    public Uri? RemoteCosmosEndpoint { get; set; }
    public string? LocalCosmosKey { get; set; } 
    public string? RemoteCosmosKey { get; set; }
    public string? CosmosRemoteDatabaseName { get; set; }
    public string? CosmosLocalDatabaseName { get; set; }
    public string? CosmosRemoteContainerName { get; set; }
    public string? CosmosLocalContainerName { get; set; }
    public string? CosmosPartitionKey { get; set; }
    public int? CosmosMaxRetryAttempts { get; set; }
    public int? CosmosRateLimitingDelay { get; set; }

}

public class AzureSettings
{
    public Guid SubscriptionId { get; set; }
    public Guid TenantId { get; set; }
    public string AzureEnvironmentPrefix { get; set; }
}