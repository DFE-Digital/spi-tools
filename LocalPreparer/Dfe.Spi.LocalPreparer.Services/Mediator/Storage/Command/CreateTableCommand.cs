using Dfe.Spi.LocalPreparer.Azure.AzureStorage;
using Dfe.Spi.LocalPreparer.Domain.Models;
using MediatR;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.Storage.Command;

public class CreateTableCommand : IRequest<bool>
{
    private readonly string TableName;

    public CreateTableCommand(string tableName)
    {
        TableName = tableName;
    }

    public class Handler : IRequestHandler<CreateTableCommand, bool>
    {
        private readonly IAzureStorageClientService _azureStorageClientService;

        public Handler(IAzureStorageClientService azureStorageClientService)
        {
            _azureStorageClientService = azureStorageClientService;
        }

        public async Task<bool> Handle(CreateTableCommand request,
            CancellationToken cancellationToken)
        {

            try
            {
                var tableClient = await _azureStorageClientService.GetTableClient(request.TableName);
                await tableClient.CreateIfNotExistsAsync(cancellationToken);
            }
            catch (Exception e)
            {
                if (e is SpiException spiException)
                {
                    throw new SpiException(spiException.Errors, e);
                }
                throw new SpiException(new List<string>()
                {
                    $"Failed to create a table '{request.TableName}'",
                }, e);
            }
            return true;
        }

    }
}