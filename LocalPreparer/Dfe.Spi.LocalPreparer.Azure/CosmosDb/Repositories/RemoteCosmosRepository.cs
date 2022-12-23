using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;
public class RemoteCosmosRepository : CosmosRepositoryBase<CosmosRegisteredEntity>, IRemoteCosmosRepository
{
    public RemoteCosmosRepository(ICosmosContainerProvider containerProvider, ILogger<RemoteCosmosRepository> logger, IOptions<SpiSettings> configuration, IContextManager contextManager) : base(containerProvider, logger, CosmosConnectionType.Remote, configuration, contextManager)
    {
    }
}