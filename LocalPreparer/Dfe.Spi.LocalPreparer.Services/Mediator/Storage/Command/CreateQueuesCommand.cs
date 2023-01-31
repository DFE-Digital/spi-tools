using Dfe.Spi.LocalPreparer.Azure.AzureStorage;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Common.Presentation;
using Dfe.Spi.LocalPreparer.Domain.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.Storage.Command;

public class CreateQueuesCommand : IRequest<bool>
{

    public class Handler : IRequestHandler<CreateQueuesCommand, bool>
    {
        private readonly IOptions<SpiSettings> _configuration;
        private readonly IContextManager _contextManager;
        private readonly IAzureStorageClientService _azureStorageClientService; 

        public Handler(IOptions<SpiSettings> configuration, IContextManager contextManager, IAzureStorageClientService azureStorageClientService)
        {
            _configuration = configuration;
            _contextManager = contextManager;
            _azureStorageClientService = azureStorageClientService;
        }

        public async Task<bool> Handle(CreateQueuesCommand request,
            CancellationToken cancellationToken)
        {
            var serviceName = _contextManager.Context.ActiveService;
          
            try
            {
                if (_configuration.Value.Services?.GetValueOrDefault(serviceName)?.Queues == null || !_configuration.Value.Services.GetValueOrDefault(serviceName).Queues.Any())
                {
                    Interactions.WriteColourLine($"{Environment.NewLine}No queue name have been configured for this service! {Environment.NewLine}", ConsoleColor.Magenta);
                    return false;
                }

                await _azureStorageClientService.CheckConnections();

                foreach (var queueName in _configuration.Value.Services?.GetValueOrDefault(serviceName)?.Queues ?? new string[] { })
                {
                    Interactions.WriteColourLine($"{Environment.NewLine}Creating \"{queueName}\" queue...{Environment.NewLine}", ConsoleColor.Blue);
                    await (await _azureStorageClientService.GetQueueClient(queueName)).CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                    Interactions.WriteColourLine($"Creating \"{queueName}\" queue succeeded! {Environment.NewLine}", ConsoleColor.Green);
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
                    "Failed to create queues!",
                }, e);
            }
        }
    }
}