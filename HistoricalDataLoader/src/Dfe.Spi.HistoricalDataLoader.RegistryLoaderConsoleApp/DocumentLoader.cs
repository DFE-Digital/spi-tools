using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataLoader.Common;
using Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp.Models;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Serilog;

namespace Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp
{
    public class DocumentLoader
    {
        private readonly string _dataDirectory;
        private readonly ILogger _logger;
        private Container _container;

        public DocumentLoader(
            string dataDirectory,
            string cosmosUri,
            string cosmosKey,
            string cosmosDbName,
            string cosmosContainerName,
            ILogger logger)
        {
            _dataDirectory = dataDirectory;
            _logger = logger;
            var client = new CosmosClient(
                cosmosUri,
                cosmosKey,
                new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                        IgnoreNullValues = true,
                    },
                });
            _container = client.GetDatabase(cosmosDbName).GetContainer(cosmosContainerName);
        }

        internal async Task LoadAsync(CancellationToken cancellationToken)
        {
            var index = await LoadIndexAsync();

            var entityTypes = index.Keys.OrderBy(x => x).ToArray();
            foreach (var entityType in entityTypes)
            {
                var documentIds = index[entityType];
                await ProcessEntityTypeAsync(entityType, documentIds, cancellationToken);
            }
        }

        private async Task<Dictionary<string, List<string>>> LoadIndexAsync()
        {
            var path = Path.Combine(_dataDirectory, "entity-type-index.json");
            return await FileSystemHelper.ReadFileAsAsync<Dictionary<string, List<string>>>(path);
        }

        private async Task ProcessEntityTypeAsync(string entityType, List<string> documentIds, CancellationToken cancellationToken)
        {
            const int maxDocumentCount = 100;
            const long maxRequestSize = 700000;
            //                          7282423

            var batch = _container.CreateTransactionalBatch(new PartitionKey(entityType));
            var batchDocumentCount = 0;
            var batchSize = 0L;
            for (var i = 0; i < documentIds.Count; i++)
            {
                var documentId = documentIds[i];

                _logger.Information("Processing {EntityType} {Index} of {NumDocuments}",
                    entityType, i + 1, documentIds.Count);

                var documentPath = Path.Combine(_dataDirectory, $"{documentId}.json");
                var registeredEntity = await FileSystemHelper.ReadFileAsAsync<RegisteredEntity>(documentPath);
                var documentSize = SizeOf(registeredEntity);
                var entityAdded = false;

                if (batchSize + documentSize < maxRequestSize)
                {
                    batch.UpsertItem(registeredEntity);
                    batchDocumentCount++;
                    batchSize += documentSize;
                    entityAdded = true;
                }
                else
                {
                    _logger.Debug("{EntityType} {Index} not added before upload as it is {DocumentSize} and the batch is already {BatchSize}. " +
                                  "Adding it would make the batch {PredictedBatchSize}, which would have exceeded the total size of {MaxRequestSize}",
                        entityType, i + 1, documentSize, batchSize, batchSize + documentSize, maxRequestSize);
                }

                if (batchDocumentCount == maxDocumentCount || !entityAdded)
                {
                    _logger.Information("Pushing batch of {BatchSize} {EntityType} (size: {BatchSizeBytes})",
                        batchDocumentCount, entityType, batchSize);

                    using var batchResponse = await batch.ExecuteAsync(cancellationToken);
                    if (!batchResponse.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to store batch of entities. {batchResponse.StatusCode} - {batchResponse.ErrorMessage}");
                    }

                    batch = _container.CreateTransactionalBatch(new PartitionKey(entityType));
                    batchDocumentCount = 0;
                    batchSize = 0;
                }

                if (!entityAdded)
                {
                    batch.UpsertItem(registeredEntity);
                    batchDocumentCount++;
                    batchSize += documentSize;
                    
                    _logger.Debug("{EntityType} {Index} added now batch has been flushed",
                        entityType, i + 1);
                }
            }

            if (batchDocumentCount > 0)
            {
                _logger.Information("Pushing batch of {BatchSize} {EntityType}",
                    batchDocumentCount, entityType);

                using var batchResponse = await batch.ExecuteAsync(cancellationToken);
                if (!batchResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to store batch of entities. {batchResponse.StatusCode} - {batchResponse.ErrorMessage}");
                }
            }
        }

        private long SizeOf(RegisteredEntity item)
        {
            var json = JsonConvert.SerializeObject(item);
            return json.Length;
        }
    }
}