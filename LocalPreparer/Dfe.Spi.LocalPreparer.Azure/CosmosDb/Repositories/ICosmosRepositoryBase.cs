using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;

public interface ICosmosRepositoryBase<TItem> where TItem : IItem
{

    ValueTask<TItem> GetAsync(
        string id,
        PartitionKey partitionKey,
        CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<TItem>> GetAsync(
        Expression<Func<TItem, bool>> predicate,
        CancellationToken cancellationToken = default);

    ValueTask<int> CountAsync(
        Expression<Func<TItem, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<ItemResponse<TItem>> CreateAsync(
        TItem value,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResponse<TItem>> BulkCreateAsync(
        IEnumerable<TItem> values,
        CancellationToken cancellationToken = default);

    Task<ItemResponse<TItem>> UpsertAsync(
        TItem value,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResponse<TItem>> BulkUpsertAsync(
        IEnumerable<TItem> values,
        CancellationToken cancellationToken = default);
}
