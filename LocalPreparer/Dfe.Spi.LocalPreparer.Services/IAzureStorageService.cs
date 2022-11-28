using Dfe.Spi.LocalPreparer.Common.Enums;

namespace Dfe.Spi.LocalPreparer.Services
{
    public interface IAzureStorageService
    {
        bool CheckConnections(ServiceName serviceName);
        Task CopyBlobAsync(ServiceName serviceName);
        Task CopyBlobToTableAsync(ServiceName serviceName);
        Task CopyTableToBlobAsync(ServiceName serviceName);
        Task CreateQueuesAsync(ServiceName serviceName);
    }
}