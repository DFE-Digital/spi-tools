using Dfe.Spi.LocalPreparer.Azure.AzureStorage;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Common.Utils;
using Dfe.Spi.LocalPreparer.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.Storage.Command;

public class CopyBlobToTableCommand : IRequest<bool>
{

    public class Handler : IRequestHandler<CopyBlobToTableCommand, bool>
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

        public async Task<bool> Handle(CopyBlobToTableCommand request,
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
                    Interactions.WriteColourLine($"{Environment.NewLine}Copying \"{tableName}\" blob into a table...{Environment.NewLine}", ConsoleColor.Blue);
                    var arguments =
                        $@"/source:{_configuration.Value.Services?.GetValueOrDefault(serviceName)?.LocalStorageBlobEndpoint}/{tempContainerName} /sourceKey:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey} /dest:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageTableEndpoint}/{tableName} /Destkey:{_configuration.Value.Services.GetValueOrDefault(serviceName).LocalStorageAccessKey} /manifest:{uniqueTargetFileName}{tableName}.manifest /sourceType:blob /destType:table /EntityOperation:InsertOrReplace";

                    var succeeded = await AzCopyLauncher.RunAsync(arguments, _logger);
                    if (!succeeded)
                        break;
                    Interactions.WriteColourLine($"{Environment.NewLine}Copying \"{tableName}\" into a table succeeded! {Environment.NewLine}", ConsoleColor.Green);

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
                    "Failed to copy blob to table!",
                }, e);
            }
        }
    }
}