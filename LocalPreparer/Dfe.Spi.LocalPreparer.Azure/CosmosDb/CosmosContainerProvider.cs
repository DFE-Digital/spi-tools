using System.Configuration;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Container = Microsoft.Azure.Cosmos.Container;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb;
public class CosmosContainerProvider : ICosmosContainerProvider
{
    private readonly ICosmosLocalClientProvider _cosmosLocalClientProvider;
    private readonly ICosmosRemoteClientProvider _cosmosRemoteClientProvider;

    private readonly ILogger<CosmosContainerProvider> _logger;
    private readonly IOptions<SpiSettings> _configuration;
    private readonly IContextManager _contextManager;
    private readonly ServiceName _currentService;

    public CosmosContainerProvider(ILogger<CosmosContainerProvider> logger, IOptions<SpiSettings> configuration, IContextManager contextManager, ICosmosLocalClientProvider cosmosLocalClientProvider, ICosmosRemoteClientProvider cosmosRemoteClientProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _contextManager = contextManager;
        _cosmosLocalClientProvider = cosmosLocalClientProvider;
        _cosmosRemoteClientProvider = cosmosRemoteClientProvider;
        _currentService = contextManager.Context.ActiveService;
    }

    public async Task<Container> GetContainerAsync(CosmosConnectionType connectionType)
    {
        try
        {
            Container container;
            var partitionKey = _configuration.Value.Services?.GetValueOrDefault(_currentService)?
                .CosmosPartitionKey;

            switch (connectionType)
            {
                case CosmosConnectionType.Local:

                    var localDbName = _configuration.Value.Services?.GetValueOrDefault(_currentService)?
                        .CosmosLocalDatabaseName;
                    var localContainerName = _configuration.Value.Services?.GetValueOrDefault(_currentService)?
                        .CosmosLocalContainerName;
                    var databaseResponse = await _cosmosLocalClientProvider.UseClientAsync(async client =>
                        (await client).CreateDatabaseIfNotExistsAsync(localDbName));
                    var localDb = (await databaseResponse).Database;
                    container = await localDb
                        .CreateContainerIfNotExistsAsync(localContainerName, partitionKey, 20000);
                    await container.ReadContainerAsync();
                    break;
                case CosmosConnectionType.Remote:

                    var remoteDbName = _configuration.Value.Services?.GetValueOrDefault(_currentService)?
                        .CosmosRemoteDatabaseName;
                    var remoteContainerName = _configuration.Value.Services?.GetValueOrDefault(_currentService)?
                        .CosmosRemoteContainerName;
                    var remoteDb = await _cosmosRemoteClientProvider.UseClientAsync(async client =>
                        (await client).GetDatabase(remoteDbName));

                    container = await Task.FromResult(remoteDb.GetContainer(remoteContainerName));
                    await container.ReadContainerAsync();

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionType), connectionType, null);
            }

            return container;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, nameof(GetContainerAsync));
            if (ex.Message.Contains("firewall"))
            {
                throw new SpiException(new List<string>()
                    { "Please make sure your IP address is whitelisted in Azure CosmosDb firewall!" }, ex);
            }
            throw new SpiException(new List<string>() { "Failed to get container!" }, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get container!");
            throw new SpiException(new List<string>(){"Failed to get container!"}, ex);
        }
    }

    public async Task<Task> PrepareLocalDatabase()
    {
        _logger.LogInformation($"Creating database \"{_configuration.Value.Services?.GetValueOrDefault(_currentService)?.CosmosLocalDatabaseName}\" if it doesn't exist!");
        _logger.LogInformation($"Recreating local CosmosDb container: {_configuration.Value.Services?.GetValueOrDefault(_currentService)?.CosmosLocalContainerName}");
        // making sure the container is deleted and recreated
        var container = await GetContainerAsync(CosmosConnectionType.Local);
        await container.DeleteContainerAsync();
        await GetContainerAsync(CosmosConnectionType.Local);
        _logger.LogInformation($"Container \"{_configuration.Value.Services?.GetValueOrDefault(_currentService)?.CosmosLocalContainerName}\" created successfully!");
        return Task.CompletedTask;
    }

}
