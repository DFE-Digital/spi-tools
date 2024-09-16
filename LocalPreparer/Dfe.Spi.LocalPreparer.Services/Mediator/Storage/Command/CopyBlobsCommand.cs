using Dfe.Spi.LocalPreparer.Azure.AzureStorage;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.Storage.Command;

public class CopyBlobsCommand : IRequest<bool>
{

    public class Handler : IRequestHandler<CopyBlobsCommand, bool>
    {
        private readonly IOptions<SpiSettings> _configuration;
        private readonly IContextManager _contextManager;
        private readonly IAzureStorageClientService _azureStorageClientService;
        private readonly ILogger<Handler> _logger;

        public Handler(IOptions<SpiSettings> configuration, IContextManager contextManager, IAzureStorageClientService azureStorageClientService, ILogger<Handler> logger)
        {
            _configuration = configuration;
            _contextManager = contextManager;
            _azureStorageClientService = azureStorageClientService;
            _logger = logger;
        }

        public async Task<bool> Handle(CopyBlobsCommand request,
            CancellationToken cancellationToken)
        {
            var serviceName = _contextManager.Context.ActiveService;

            try
            {
                if (_configuration.Value.Services?.GetValueOrDefault(serviceName)?.BlobContainers == null || !_configuration.Value.Services.GetValueOrDefault(serviceName).BlobContainers.Any())
                {
                    Interactions.WriteColourLine($"{Environment.NewLine}No blob container name have been configured for this service! {Environment.NewLine}", ConsoleColor.Magenta);
                    return false;
                }

                await _azureStorageClientService.CheckConnections();

                foreach (var blobContainerName in _configuration.Value.Services?.GetValueOrDefault(serviceName)?.BlobContainers ?? new string[] { })
                {
                    await (await _azureStorageClientService.GetBlobContainerClient(blobContainerName)).CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                    Interactions.WriteColourLine(
                        $"{Environment.NewLine}Blob container \"{blobContainerName}\" created! {Environment.NewLine}",
                        ConsoleColor.Green);

                    Interactions.WriteColourLine(
                        $"{Environment.NewLine}Copying all blobs from \"{blobContainerName}\"...{Environment.NewLine}",
                        ConsoleColor.Blue);
                    var arguments =
                        $@"/source:https://{_configuration.Value.Services?.GetValueOrDefault(serviceName)?.RemoteStorageAccountName}.blob.core.windows.net/{blobContainerName} /sourceKey:{await _azureStorageClientService.GetAzureStorageKeyAsync()} /dest:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageBlobEndpoint}/{blobContainerName} /Destkey:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey} /destType:blob /S";

                    var succeeded = await AzCopyLauncher.RunAsync(arguments, _logger);
                    if (!succeeded)
                        break;
                    Interactions.WriteColourLine(
                        $"{Environment.NewLine}Copying blobs from \"{blobContainerName}\" succeeded! {Environment.NewLine}",
                        ConsoleColor.Green);
                }

                foreach (var blobContainerName in _configuration.Value.Services?.GetValueOrDefault(serviceName)
                             ?.BlobContainersWithoutContent ?? new string[] { })
                {
                    await (await _azureStorageClientService.GetBlobContainerClient(blobContainerName)).CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                    Interactions.WriteColourLine(
                        $"{Environment.NewLine}Blob container \"{blobContainerName}\" created! {Environment.NewLine}",
                        ConsoleColor.Green);
                }


                return true;
            }
            catch (Exception e)
            {
                if (e is SpiException spiException)
                {
                    throw new SpiException(spiException.Errors, e);
                }
                throw new SpiException(new List<string>()
                {
                    "Failed to copy blobs!",
                }, e);

            }
        }
    }
}