using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Extensions.Logging;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories
{
    public class LocalCosmosRepository : CosmosRepositoryBase<CosmosRegisteredEntity>, ILocalCosmosRepository
    {
        public LocalCosmosRepository(ICosmosContainerProvider containerProvider, ILogger<LocalCosmosRepository> logger) : base(containerProvider, logger, CosmosConnectionType.Local)
        {
        }
    }
}
