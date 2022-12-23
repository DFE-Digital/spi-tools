using Dfe.Spi.LocalPreparer.Azure.AzureStorage;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.Storage.Command;

public class CopyTableToBlobCommand : IRequest<bool>
{

    public class Handler : IRequestHandler<CopyTableToBlobCommand, bool>
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

        public async Task<bool> Handle(CopyTableToBlobCommand request,
            CancellationToken cancellationToken)
        {
            var serviceName = _contextManager.Context.ActiveService;

            try
            {
                if (_configuration.Value.Services?.GetValueOrDefault(serviceName)?.Tables == null || !_configuration.Value.Services.GetValueOrDefault(serviceName).Tables.Any())
                {
                    Interactions.WriteColourLine($"{Environment.NewLine}No table name have been configured for this service! {Environment.NewLine}", ConsoleColor.Magenta);
                    return false;
                }

                var tempContainerName = $"temp-{serviceName.ToString().ToLower()}";
                var uniqueTargetFileName = $"{serviceName}-";

                await _azureStorageClientService.CheckConnections();

                // Create a temporary blob container for table blobs
                await (await _azureStorageClientService.GetBlobContainerClient(tempContainerName)).CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                foreach (var tableName in _configuration.Value.Services?.GetValueOrDefault(serviceName)?.Tables ?? new string[] { })
                {
                    Interactions.WriteColourLine($"{Environment.NewLine}Downloading \"{tableName}\" as a blob into {tempContainerName} container...{Environment.NewLine}", ConsoleColor.Blue);
                    var arguments =
                        $@"/source:https://{_configuration.Value.Services?.GetValueOrDefault(serviceName)?.RemoteStorageAccountName}.table.core.windows.net/{tableName} /sourceKey:{await _azureStorageClientService.GetAzureStorageKeyAsync()} /dest:{_configuration.Value.Services?.GetValueOrDefault(serviceName).LocalStorageBlobEndpoint}/{tempContainerName} /Destkey:{_configuration.Value.Services?.GetValueOrDefault(serviceName).LocalStorageAccessKey} /destType:blob /manifest:{uniqueTargetFileName}{tableName} /SplitSize:128";

                    var succeeded = await AzCopyLauncher.RunAsync(arguments, _logger);
                    if (!succeeded)
                        break;
                    Interactions.WriteColourLine($"{Environment.NewLine}Downloading \"{tableName}\" as a blob succeeded! {Environment.NewLine}", ConsoleColor.Green);
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
                    "Failed to copy table to blob!",
                }, e);
            }
        }
    }
}