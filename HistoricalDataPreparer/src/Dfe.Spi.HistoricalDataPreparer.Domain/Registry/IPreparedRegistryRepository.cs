using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Registry
{
    public interface IPreparedRegistryRepository
    {
        Task<RegisteredEntity> GetRegisteredEntityAsync(
            string type, 
            string sourceSystemName, 
            string sourceSystemId, 
            DateTime date,
            CancellationToken cancellationToken);

        Task StoreRegisteredEntityAsync(RegisteredEntity entity, DateTime dateTime, CancellationToken cancellationToken);

        void DeleteRegisteredEntity(string id);

        Task FlushAsync();
    }
}