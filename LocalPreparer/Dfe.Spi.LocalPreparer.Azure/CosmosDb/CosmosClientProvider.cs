using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.Resources;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb;
public abstract class CosmosClientProvider : ICosmosClientProvider, IDisposable
{
    private Lazy<Task<CosmosClient>> _lazyCosmosClient;
    private readonly IOptions<SpiSettings> _configuration;
    private readonly IContextManager _contextManager;
    private readonly ILogger<CosmosClientProvider> _logger;

    public CosmosClientProvider(
        IOptions<SpiSettings> configuration, IContextManager contextManager, CosmosConnectionType connectionType, ILogger<CosmosClientProvider> logger)
    {
        _configuration = configuration;
        _contextManager = contextManager;
        _logger = logger;
        _lazyCosmosClient = new Lazy<Task<CosmosClient>>(async () => await GetCosmosClientAsync(_contextManager.Context.ActiveService, connectionType));

    }

    /// <summary>
    /// Creates and return a cosmosDb client
    /// </summary>
    /// <param name="service"></param>
    /// <param name="connectionType"></param>
    /// <returns></returns>
    private async Task<CosmosClient> GetCosmosClientAsync(ServiceName service, CosmosConnectionType connectionType)
    {
        var cosmosClient = connectionType switch
        {
            CosmosConnectionType.Local => new CosmosClient(
                _configuration.Value.Services?.GetValueOrDefault(service)?.LocalCosmosEndpoint.ToString(),
                _configuration.Value.Services?.GetValueOrDefault(service)?.LocalCosmosKey, new CosmosClientOptions()
                {
                    SerializerOptions = new CosmosSerializationOptions()
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    },
                    AllowBulkExecution = true,
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = TimeSpan.FromMinutes(1),
                    EnableContentResponseOnWrite = false,

                }),
            CosmosConnectionType.Remote => new CosmosClient(
                _configuration.Value.Services?.GetValueOrDefault(service)?.RemoteCosmosEndpoint.ToString(),
                await GetAzureCosmosKeyAsync(), new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    SerializerOptions = new CosmosSerializationOptions()
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    }
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(connectionType), connectionType, null)
        };
        return cosmosClient;
    }

    public Task<T> UseClientAsync<T>(Func<Task<CosmosClient>, Task<T>> consume)
    {
        return consume.Invoke(_lazyCosmosClient.Value);
    }

    public void Dispose()
    {
        if (!_lazyCosmosClient.IsValueCreated) return;
        _lazyCosmosClient.Value.Dispose();
    }

    private async Task<string?> GetAzureCosmosKeyAsync()
    {
        try
        {
            var serviceName = _contextManager.Context.ActiveService;

            if (!string.IsNullOrEmpty(_configuration.Value.Services?.GetValueOrDefault(serviceName)?.RemoteCosmosKey))
            {
                return _configuration.Value.Services?.GetValueOrDefault(serviceName)?.RemoteCosmosKey;
            }

            if (_contextManager.Context.CosmosDbAccountKeys.TryGetValue(serviceName,
                    out string? cachedKey)) return cachedKey;

            Interactions.WriteColourLine(
                $"{Environment.NewLine}Retrieving CosmosDb Account details for {serviceName}...{Environment.NewLine}",
                ConsoleColor.Blue);

            var accountName = _configuration.Value.Services?.GetValueOrDefault(serviceName)
                ?.RemoteCosmosAccountName;
            var cosmosDbResourceGroup = $"{_configuration.Value.Azure.AzureEnvironmentPrefix}-{serviceName.ToString().ToLower()}";
            var subscription = _contextManager.Context.SubscriptionResource;
            var resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(cosmosDbResourceGroup);
            var cosmosDbAccount = await resourceGroup.GetCosmosDBAccountAsync(accountName);
            var key1 = (await cosmosDbAccount.Value.GetKeysAsync()).Value.PrimaryMasterKey;
            if (key1 == null)
                throw new Exception();
            _contextManager.Context.CosmosDbAccountKeys.Add(serviceName, key1);
            return key1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetAzureCosmosKeyAsync));
            throw new SpiException("Failed to retrieve CosmosDb Account Access Key!", ex);
        }
    }
}
