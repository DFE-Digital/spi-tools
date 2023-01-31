using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb;
public class CosmosLocalClientProvider : CosmosClientProvider, ICosmosLocalClientProvider
{
    public CosmosLocalClientProvider(IOptions<SpiSettings> configuration, IContextManager contextManager, ILogger<CosmosLocalClientProvider> logger) : base(configuration, contextManager, CosmosConnectionType.Local, logger)
    {
    }
}

