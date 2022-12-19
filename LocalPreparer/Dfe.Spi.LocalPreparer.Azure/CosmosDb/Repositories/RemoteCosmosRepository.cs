using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Extensions.Logging;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories
{
    public class RemoteCosmosRepository : CosmosRepositoryBase<CosmosRegisteredEntity>, IRemoteCosmosRepository
    {
        public RemoteCosmosRepository(ICosmosContainerProvider containerProvider, ILogger<RemoteCosmosRepository> logger) : base(containerProvider, logger, CosmosConnectionType.Remote)
        {
        }
    }
}
