using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Dfe.Spi.LocalPreparer.Common.Configurations;
using Dfe.Spi.LocalPreparer.Common.Enums;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Services;
public class AzureStorageService : IAzureStorageService
{

    private readonly IOptions<SpiSettings> _configuration;
    private readonly ILogger<AzureStorageService> _logger;

    public AzureStorageService(IOptions<SpiSettings> configuration, ILogger<AzureStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CopyTableToBlobAsync(ServiceName serviceName)
    {
        try
        {
            if (_configuration.Value.Services.GetValueOrDefault(serviceName)?.Tables == null || !_configuration.Value.Services.GetValueOrDefault(serviceName).Tables.Any())
            {
                Interactions.WriteColourLine($"{Environment.NewLine}No table name have been configured for this service! {Environment.NewLine}", ConsoleColor.Magenta);
                return;
            }

            var tempContainerName = $"temp-{serviceName.ToString().ToLower()}";
            var uniqueTargetFileName = $"{serviceName}-";

            if (!await CheckConnections(serviceName))
                return;

            // Create a temporary blob container for table blobs
            await CreateBlobContainerAsync(serviceName, tempContainerName);

            foreach (var tableName in _configuration.Value.Services.GetValueOrDefault(serviceName).Tables ?? new string[] { })
            {
                Interactions.WriteColourLine($"{Environment.NewLine}Downloading \"{tableName}\" as a blob into {tempContainerName} container...{Environment.NewLine}", ConsoleColor.Blue);
                var arguments =
                              $@"/source:https://{_configuration.Value.Services.GetValueOrDefault(serviceName).RemoteStorageAccountName}.table.core.windows.net/{tableName} /sourceKey:{_configuration.Value.Services.GetValueOrDefault(serviceName).RemoteStorageAccessKey} /dest:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageBlobEndpoint}/{tempContainerName} /Destkey:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey} /destType:blob /manifest:{uniqueTargetFileName}{tableName} /SplitSize:128";

                var succeeded = await AzCopyLauncher.RunAsync(arguments, _logger);
                if (!succeeded)
                    break;
                Interactions.WriteColourLine($"{Environment.NewLine}Downloading \"{tableName}\" as a blob succeeded! {Environment.NewLine}", ConsoleColor.Green);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _logger.LogError($"{nameof(CopyTableToBlobAsync)} > Exception: {e}");
        }
    }

    public async Task CopyBlobToTableAsync(ServiceName serviceName)
    {
        try
        {
            if (_configuration.Value.Services.GetValueOrDefault(serviceName).Tables == null || !_configuration.Value.Services.GetValueOrDefault(serviceName).Tables.Any())
            {
                Interactions.WriteColourLine($"{Environment.NewLine}No table name have been configured for this service! {Environment.NewLine}", ConsoleColor.Magenta);
                return;
            }

            var tempContainerName = $"temp-{serviceName.ToString().ToLower()}";
            var uniqueTargetFileName = $"{serviceName}-";

            if (!await CheckConnections(serviceName))
                return;

            // Create a temporary blob container for table blobs
            await CreateBlobContainerAsync(serviceName, tempContainerName);

            foreach (var tableName in _configuration.Value.Services.GetValueOrDefault(serviceName).Tables ?? new string[] { })
            {
                Interactions.WriteColourLine($"{Environment.NewLine}Copying \"{tableName}\" blob into a table...{Environment.NewLine}", ConsoleColor.Blue);
                var arguments =
                              $@"/source:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageBlobEndpoint}/{tempContainerName} /sourceKey:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey} /dest:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageTableEndpoint}/{tableName} /Destkey:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey} /manifest:{uniqueTargetFileName}{tableName}.manifest /sourceType:blob /destType:table /EntityOperation:InsertOrReplace";

                var succeeded = await AzCopyLauncher.RunAsync(arguments, _logger);
                if (!succeeded)
                    break;
                Interactions.WriteColourLine($"{Environment.NewLine}Copying \"{tableName}\" into a table succeeded! {Environment.NewLine}", ConsoleColor.Green);

            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _logger.LogError($"{nameof(CopyBlobToTableAsync)} > Exception: {e}");
        }
    }

    public async Task CreateQueuesAsync(ServiceName serviceName)
    {
        try
        {
            if (_configuration.Value.Services.GetValueOrDefault(serviceName).Queues == null || !_configuration.Value.Services.GetValueOrDefault(serviceName).Queues.Any())
            {
                Interactions.WriteColourLine($"{Environment.NewLine}No queue name have been configured for this service! {Environment.NewLine}", ConsoleColor.Magenta);
                return;
            }
            if (!await CheckConnections(serviceName))
                return;

            foreach (var queueName in _configuration.Value.Services.GetValueOrDefault(serviceName).Queues ?? new string[] { })
            {
                Interactions.WriteColourLine($"{Environment.NewLine}Creating \"{queueName}\" queue...{Environment.NewLine}", ConsoleColor.Blue);
                await CreateQueueAsync(serviceName, queueName);
                Interactions.WriteColourLine($"Creating \"{queueName}\" queue succeeded! {Environment.NewLine}", ConsoleColor.Green);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _logger.LogError($"{nameof(CreateQueuesAsync)} > Exception: {e}");
        }
    }

    private async Task CreateBlobContainerAsync(ServiceName serviceName, string blobContainerName)
    {
        try
        {
            var blobContainerClient = new BlobContainerClient(CreateConnectionString(serviceName), blobContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();
        }
        catch (Exception e)
        {
            Interactions.RaiseError(new List<string>() { "Failed to create a blob container, please check your connection string!" }, null);
        }
    }

    private async Task CreateQueueAsync(ServiceName serviceName, string queueName)
    {
        try
        {
            var queueClient = new QueueClient(CreateConnectionString(serviceName), queueName);
            await queueClient.CreateIfNotExistsAsync();
        }
        catch (Exception e)
        {
            Interactions.RaiseError(new List<string>() { $"Failed to create \"{queueName}\" queue, please check your connection string!" }, null);
        }
    }

    public async Task CopyBlobAsync(ServiceName serviceName)
    {
        try
        {
            if (_configuration.Value.Services.GetValueOrDefault(serviceName).BlobContainers == null || !_configuration.Value.Services.GetValueOrDefault(serviceName).BlobContainers.Any())
            {
                Interactions.WriteColourLine($"{Environment.NewLine}No blob container name have been configured for this service! {Environment.NewLine}", ConsoleColor.Magenta);
                return;
            }

            if (!await CheckConnections(serviceName))
                return;

            foreach (var blobContainerName in _configuration.Value.Services.GetValueOrDefault(serviceName).BlobContainers ?? new string[] { })
            {
                await CreateBlobContainerAsync(serviceName, blobContainerName);

                if (!(_configuration.Value.Services.GetValueOrDefault(serviceName).CopyBlobContainerContents ??
                      true)) continue;

                Interactions.WriteColourLine(
                    $"{Environment.NewLine}Copying all blobs from \"{blobContainerName}\"...{Environment.NewLine}",
                    ConsoleColor.Blue);
                var arguments =
                    $@"/source:https://{_configuration.Value.Services.GetValueOrDefault(serviceName).RemoteStorageAccountName}.blob.core.windows.net/{blobContainerName} /sourceKey:{_configuration.Value.Services.GetValueOrDefault(serviceName).RemoteStorageAccessKey} /dest:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageBlobEndpoint}/{blobContainerName} /Destkey:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey} /destType:blob /S";

                var succeeded = await AzCopyLauncher.RunAsync(arguments, _logger);
                if (!succeeded)
                    break;
                Interactions.WriteColourLine(
                    $"{Environment.NewLine}Copying blobs from \"{blobContainerName}\" succeeded! {Environment.NewLine}",
                    ConsoleColor.Green);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _logger.LogError($"{nameof(CopyBlobAsync)} > Exception: {e}");
        }
    }

    public async Task<bool> CheckConnections(ServiceName serviceName)
    {

        var errors = new List<string>();
        try
        {
            var blobContainerClient = new BlobContainerClient(CreateConnectionString(serviceName), "test");
            await blobContainerClient.ExistsAsync();
        }
        catch (Exception e)
        {
            errors.Add("Connection to the Local Azure Storage failed. Please check your Access Key or connection string!");
        }

        try
        {
            var blobContainerClient = new BlobContainerClient(CreateConnectionString(serviceName, true), "test");
            await blobContainerClient.ExistsAsync();
        }
        catch (Exception e)
        {
            errors.Add("Connection to the Remote Azure Storage failed. Please check your Access Key or connection string!");
        }
        if (!errors.Any())
            return true;
        Interactions.RaiseError(errors, null);
        return false;

    }

    private string CreateConnectionString(ServiceName serviceName, bool remote = false)
    {
        if (remote)
            return $"DefaultEndpointsProtocol=https;AccountName={_configuration.Value.Services.GetValueOrDefault(serviceName).RemoteStorageAccountName};AccountKey={_configuration.Value.Services.GetValueOrDefault(serviceName).RemoteStorageAccessKey};EndpointSuffix=core.windows.net";
        return $"AccountName={_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccountName};" +
            $"AccountKey={_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey};" +
            $"DefaultEndpointsProtocol=http;" +
            $"BlobEndpoint={_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageBlobEndpoint};" +
            $"QueueEndpoint={_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageQueueEndpoint};" +
            $"TableEndpoint={_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageTableEndpoint};";
    }
}