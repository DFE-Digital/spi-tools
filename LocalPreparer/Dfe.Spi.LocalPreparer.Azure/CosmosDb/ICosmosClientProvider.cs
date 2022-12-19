using Dfe.Spi.LocalPreparer.Domain.Enums;
using Microsoft.Azure.Cosmos;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb
{
    public interface ICosmosClientProvider
    {
        Task<T> UseClientAsync<T>(Func<Task<CosmosClient>, Task<T>> consume);
    }
}
