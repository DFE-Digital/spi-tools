using Dfe.Spi.LocalPreparer.Common.Enums;
namespace Dfe.Spi.LocalPreparer.Common.Configurations;

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
    public string[]? Queues { get; set; }
    public string[]? BlobContainers { get; set; }
    public bool? CopyBlobContainerContents { get; set; }
    public string? RemoteStorageAccessKey { get; set; }
    public string? LocalStorageAccessKey { get; set; }
    public Uri? LocalStorageBlobEndpoint { get; set; }
    public Uri? LocalStorageTableEndpoint { get; set; }
    public Uri? LocalStorageQueueEndpoint { get; set; }
}

public class AzureSettings
{
    public Guid SubscriptionId { get; set; }
}