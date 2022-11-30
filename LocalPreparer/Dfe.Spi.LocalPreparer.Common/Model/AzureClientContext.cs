using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Dfe.Spi.LocalPreparer.Common.Enums;

namespace Dfe.Spi.LocalPreparer.Common.Model;

public class AzureClientContext
{
    public Guid? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public ArmClient ArmClient { get; set; }
    public SubscriptionResource SubscriptionResource { get; set; }  
    public Dictionary<ServiceName, string> StorageAccountKeys { get; set; }
}

