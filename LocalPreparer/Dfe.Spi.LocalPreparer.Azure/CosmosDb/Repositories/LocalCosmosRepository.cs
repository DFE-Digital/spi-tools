using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;
public class LocalCosmosRepository : CosmosRepositoryBase<CosmosRegisteredEntity>, ILocalCosmosRepository
{
    public LocalCosmosRepository(ICosmosContainerProvider containerProvider, ILogger<LocalCosmosRepository> logger, IOptions<SpiSettings> configuration, IContextManager contextManager) : base(containerProvider, logger, CosmosConnectionType.Local, configuration, contextManager)
    {
    }
}

