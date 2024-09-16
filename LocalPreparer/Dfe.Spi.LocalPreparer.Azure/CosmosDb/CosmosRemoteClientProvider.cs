using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb;

public class CosmosRemoteClientProvider : CosmosClientProvider, ICosmosRemoteClientProvider
{
    public CosmosRemoteClientProvider(IOptions<SpiSettings> configuration, IContextManager contextManager, ILogger<CosmosRemoteClientProvider> logger) : base(configuration, contextManager, CosmosConnectionType.Remote, logger)
    {
    }
}