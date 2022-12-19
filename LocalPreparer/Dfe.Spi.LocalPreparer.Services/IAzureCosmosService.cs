using Dfe.Spi.LocalPreparer.Domain.Enums;

namespace Dfe.Spi.LocalPreparer.Services;

public interface IAzureCosmosService
{
    Task CopyCosmosDbData(ServiceName serviceName);
}