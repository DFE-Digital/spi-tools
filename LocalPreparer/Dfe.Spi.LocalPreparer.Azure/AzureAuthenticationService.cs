using Azure.Core;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
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

    public async Task<AzureClientContext?> AuthenticateAsync()
    {
        try
        {
            ResourceIdentifier id = new ResourceIdentifier($"/subscriptions/{_configuration.Value.Azure.SubscriptionId}");
            // uncomment for detailed authentication process
            //using AzureEventSourceListener listener = AzureEventSourceListener.CreateConsoleLogger();
            var authOptions = new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeAzureCliCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeManagedIdentityCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeInteractiveBrowserCredential = false,
                TenantId = _configuration.Value.Azure.TenantId.ToString()
            };
            var client = new ArmClient(new DefaultAzureCredential(authOptions));

            var subscriptions = client.GetSubscriptions();
            SubscriptionResource subscription = await subscriptions.GetAsync(id.SubscriptionId);

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
           Console.ReadLine();
           return null;
        }
    }
}
