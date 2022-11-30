using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Dfe.Spi.LocalPreparer.Common.Configurations;
using Dfe.Spi.LocalPreparer.Common.Enums;
using Dfe.Spi.LocalPreparer.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure;

public class AzureAuthenticationService : IAzureAuthenticationService
{
    private readonly IAzureClientContextManager _contextManager;
    private readonly IOptions<SpiSettings> _configuration;
    private readonly ILogger<AzureAuthenticationService> _logger;
    
    public AzureAuthenticationService(IAzureClientContextManager contextManager, IOptions<SpiSettings> configuration, ILogger<AzureAuthenticationService> logger)
    {
        _contextManager = contextManager;
        _configuration = configuration;
        _logger = logger;
    }

    public AzureClientContext? AuthenticateAsync()
    {
        try
        {
            var client = new ArmClient(new DefaultAzureCredential(true));
            var subscriptions = client.GetSubscriptions();
            var subscription = subscriptions.Get(_configuration.Value.Azure.SubscriptionId.ToString()).Value;
            var context = new AzureClientContext()
            {
                ArmClient = client,
                SubscriptionId = new Guid(subscription.Id.SubscriptionId),
                SubscriptionResource = subscription,
                StorageAccountKeys = new Dictionary<ServiceName, string>()
            };
            _contextManager.SetContext(context);
            return context;
        }
        catch (Exception e)
        {
           _logger.LogError($"Azure authentication failed! {Environment.NewLine} {e}");
           return null;
        }
    }
}
