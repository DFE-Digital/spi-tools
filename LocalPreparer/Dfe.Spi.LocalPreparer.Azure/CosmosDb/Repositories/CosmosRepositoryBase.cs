using Dfe.Spi.LocalPreparer.Common;
using Dfe.Spi.LocalPreparer.Domain.Enums;
using Dfe.Spi.LocalPreparer.Domain.Models;
using Dfe.Spi.LocalPreparer.Domain.Models.CosmosDb;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Dfe.Spi.LocalPreparer.Azure.CosmosDb.Repositories;

public abstract class CosmosRepositoryBase<TItem> : ICosmosRepositoryBase<TItem> where TItem : IItem
{
    private readonly ICosmosContainerProvider _containerProvider;
    private readonly IOptions<SpiSettings> _configuration;
    private readonly ILogger<CosmosRepositoryBase<TItem>> _logger;
    private readonly CosmosConnectionType _connectionType;
    private readonly Lazy<Task<Container>> _lazyContainer;
    private readonly IContextManager _contextManager;

    protected CosmosRepositoryBase(ICosmosContainerProvider containerProvider, ILogger<CosmosRepositoryBase<TItem>> logger, CosmosConnectionType connectionType, IOptions<SpiSettings> configuration, IContextManager contextManager)
    {
        _containerProvider = containerProvider;
        _logger = logger;
        _connectionType = connectionType;
        _configuration = configuration;
        _contextManager = contextManager;
        _lazyContainer = new Lazy<Task<Container>>(async () => await _containerProvider.GetContainerAsync(connectionType));
    }


    public async ValueTask<int> CountAsync(Expression<Func<TItem, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var container = await _lazyContainer.Value;
        var query =
            container.GetItemLinqQueryable<TItem>()
                .Where(predicate);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<ItemResponse<TItem>> CreateAsync(
        TItem value,
        CancellationToken cancellationToken = default)
    {
        var container = await _lazyContainer.Value;

        var response = await container.CreateItemAsync(value, new PartitionKey(value.PartitionableId),
            cancellationToken: cancellationToken);
        if (!Debugger.IsAttached)
            Thread.Sleep(_configuration.Value.Services?.GetValueOrDefault(_contextManager.Context.ActiveService)?.CosmosRateLimitingDelay ?? 1);
        return response;
    }

    public async Task<ItemResponse<TItem>> UpsertAsync(
        TItem value,
        CancellationToken cancellationToken = default)
    {
        var container = await _lazyContainer.Value;

        var response = await container.UpsertItemAsync(value, new PartitionKey(value.PartitionableId),
            cancellationToken: cancellationToken);
        await Task.Delay(_configuration.Value.Services?.GetValueOrDefault(_contextManager.Context.ActiveService)?.CosmosRateLimitingDelay ?? 1, cancellationToken);
        return response;
    }

    public async Task<BulkOperationResponse<TItem>> BulkCreateAsync(
        ReadOnlyMemory<TItem> values,
        CancellationToken cancellationToken = default)
    {
        var bulkOperations = new BulkOperations<TItem>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            bulkOperations.Tasks.Add(CaptureOperationResponse(CreateAsync(values.Span[i], cancellationToken), values.Span[i]));
        }
        var bulkOperationResponse = await bulkOperations.ExecuteAsync();
        _logger.LogInformation($"Batch create operation finished in {bulkOperationResponse.TotalTimeTaken}");
        _logger.LogInformation($"Created {bulkOperationResponse.SuccessfulDocuments} items");
        _logger.LogInformation($"Failed {bulkOperationResponse.Failures.Count} items");
        return bulkOperationResponse;
    }

    public async Task<BulkOperationResponse<TItem>> BulkUpsertAsync(
        ReadOnlyMemory<TItem> values,
        CancellationToken cancellationToken = default)
    {
        var bulkOperations = new BulkOperations<TItem>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            bulkOperations.Tasks.Add(CaptureOperationResponse(UpsertAsync(values.Span[i], cancellationToken), values.Span[i]));
        }
        var bulkOperationResponse = await bulkOperations.ExecuteAsync();
        _logger.LogInformation($"Batch upsert operation finished in {bulkOperationResponse.TotalTimeTaken}");
        _logger.LogInformation($"Created {bulkOperationResponse.SuccessfulDocuments} items");
        _logger.LogInformation($"Failed {bulkOperationResponse.Failures.Count} items");
        return bulkOperationResponse;
    }

    public async ValueTask<TItem> GetAsync(string id, PartitionKey partitionKey, CancellationToken cancellationToken = default)
    {
        var container = await _lazyContainer.Value;
        var response =
            await container.ReadItemAsync<TItem>(id, partitionKey, cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async ValueTask<ReadOnlyMemory<TItem>> GetAsync(Expression<Func<TItem, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var container = await _lazyContainer.Value;
        var count = await CountAsync(predicate, cancellationToken);
        _logger.LogInformation($"Downloading {count} items from the {_connectionType} CosmosDb...");

        var timer = new Stopwatch();
        timer.Start();

        var query = container.GetItemLinqQueryable<TItem>().Where(predicate);
        using var iterator = query.ToFeedIterator();
        var results = new List<TItem>(count);

        while (iterator.HasMoreResults)
        {
            var feedResponse = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(feedResponse.Resource);
        }
        timer.Stop();
        _logger.LogInformation("Download completed, elapsed time: " + timer.Elapsed);
        return results.ToArray().AsMemory();
    }

    private static async Task<OperationResponse<T>> CaptureOperationResponse<T>(Task<ItemResponse<T>> task, T item)
    {
        try
        {
            var response = await task;
            return new OperationResponse<T>()
            {
                Item = item,
                IsSuccessful = true,
                RequestUnitsConsumed = task.Result.RequestCharge
            };
        }
        catch (Exception ex)
        {
            if (ex is CosmosException cosmosException)
            {
                return new OperationResponse<T>()
                {
                    Item = item,
                    RequestUnitsConsumed = cosmosException.RequestCharge,
                    IsSuccessful = false,
                    CosmosException = cosmosException
                };
            }

            return new OperationResponse<T>()
            {
                Item = item,
                IsSuccessful = false,
                CosmosException = ex
            };
        }
    }

}
