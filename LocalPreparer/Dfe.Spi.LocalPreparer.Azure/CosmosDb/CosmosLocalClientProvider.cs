using Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;
using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb
{
    public class CosmosLocalClientProvider : CosmosClientProvider, ICosmosLocalClientProvider
    {
        public CosmosLocalClientProvider(IOptions<SpiSettings> configuration, IContextManager contextManager) : base(configuration, contextManager, CosmosConnectionType.Local)
        {
        }
    }
}
