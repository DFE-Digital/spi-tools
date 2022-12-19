using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Dfe.Spi.LocalPreparer.Domain.Enums;

namespace Dfe.Spi.LocalPreparer.Domain.Models;

public class Context
{
    public Guid? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public ArmClient ArmClient { get; set; }
    public SubscriptionResource SubscriptionResource { get; set; }  
    public Dictionary<ServiceName, string> StorageAccountKeys { get; set; }
    public Dictionary<ServiceName, string> CosmosDbAccountKeys { get; set; }
    public ServiceName ActiveService { get; set; }
}

