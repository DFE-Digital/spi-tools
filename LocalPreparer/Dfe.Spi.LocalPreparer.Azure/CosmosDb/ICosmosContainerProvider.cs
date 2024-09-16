using Dfe.Spi.LocalPreparer.Domain.Enums;
using Microsoft.Azure.Cosmos;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb;
public interface ICosmosContainerProvider
{
    /// <summary>
    /// Retrieves the container for the specified connection type
    /// </summary>
    /// <param name="connectionType"></param>
    /// <returns></returns>
    Task<Container> GetContainerAsync(CosmosConnectionType connectionType);
    /// <summary>
    /// Delete and recreates local container
    /// </summary>
    /// <returns></returns>
    Task<Task> PrepareLocalDatabase();
}
