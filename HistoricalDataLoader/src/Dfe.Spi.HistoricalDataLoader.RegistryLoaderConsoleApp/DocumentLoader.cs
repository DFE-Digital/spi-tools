using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataLoader.Common;
using Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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
                    },
                });
            _container = client.GetDatabase(cosmosDbName).GetContainer(cosmosContainerName);
        }

        internal async Task LoadAsync(string[] typesToExclude, CancellationToken cancellationToken)
        {
            var index = await LoadIndexAsync();

            var entityTypes = index.Keys
                .OrderBy(x => x)
                .ToArray();
            foreach (var entityType in entityTypes)
            {
                if (typesToExclude.Any(toExclude => entityType.Equals(toExclude, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.Information("Skipping processing of entity type {EntityType} as it has been excluded", entityType);
                    continue;
                }

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
            var partitionedEntities = new Dictionary<string, List<string>>();

            // Partition
            _logger.Information("Starting to batch documents of type {EntityType}", entityType);
            for (var i = 0; i < documentIds.Count; i++)
            {
                _logger.Debug("Loading document {Index} of {TotalNumberOfDocuments} of type {EntityType}",
                    i, documentIds.Count, entityType);

                var documentId = documentIds[i];
                var documentPath = Path.Combine(_dataDirectory, $"{documentId}.json");
                var registeredEntity = await FileSystemHelper.ReadFileAsAsync<CosmosRegisteredEntity>(documentPath);

                if (!partitionedEntities.ContainsKey(registeredEntity.PartitionableId))
                {
                    partitionedEntities.Add(registeredEntity.PartitionableId, new List<string>());
                }

                partitionedEntities[registeredEntity.PartitionableId].Add(documentId);
            }

            _logger.Information("Built {NumberOfBatches} batches of type {EntityType}", partitionedEntities.Count, entityType);

            // Upload
            const long maxRequestSize = 700000;

            _logger.Information("Starting to uploaded batches of type {EntityType}", entityType);
            var partitionIds = partitionedEntities.Keys.ToArray();
            for (var i = 0; i < partitionIds.Length; i++)
            {
                var partitionId = partitionIds[i];
                var registeredEntityDocumentIds = partitionedEntities[partitionId];

                var registeredEntityIdsForLog = registeredEntityDocumentIds.Aggregate((x, y) => $"{x}, {y}");
                _logger.Debug(
                    "Uploading partition {Index} ({PartitionId}) of {TotalNumberOfPartitions} of type {EntityType}; which contains {NumberOfEntitiesInPartition} entities (Ids: {RegisteredEntitiesIds}))",
                    i, partitionId, partitionIds.Length, entityType, registeredEntityDocumentIds.Count, registeredEntityIdsForLog);

                var partitionKey = new PartitionKey(partitionId);
                var registeredEntities = new CosmosRegisteredEntity[registeredEntityDocumentIds.Count];

                for (var j = 0; j < registeredEntityDocumentIds.Count; j++)
                {
                    var documentPath = Path.Combine(_dataDirectory, $"{registeredEntityDocumentIds[j]}.json");
                    registeredEntities[j] = await FileSystemHelper.ReadFileAsAsync<CosmosRegisteredEntity>(documentPath);
                }

                var potentialBatchSize = registeredEntities
                    .Select(SizeOf)
                    .Sum();
                if (potentialBatchSize >= maxRequestSize)
                {
                    _logger.Information("Batch would be too large, processing individually");
                    for (var j = 0; j < registeredEntities.Length; j++)
                    {
                        _logger.Debug("Uploading document {DocumentIndex} of {NumberOfDocuments} from partition {Index} ({PartitionId})",
                            j, registeredEntities.Length, i, partitionId);
                        await UpsertItemAsync(registeredEntities[j], partitionKey, cancellationToken: cancellationToken);
                    }

                    continue;
                }

                var batch = _container.CreateTransactionalBatch(partitionKey);
                foreach (var registeredEntity in registeredEntities)
                {
                    batch.UpsertItem(registeredEntity);
                }

                using var batchResponse = await batch.ExecuteAsync(cancellationToken);
                if (!batchResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to store batch of entities. {batchResponse.StatusCode} - {batchResponse.ErrorMessage}");
                }
            }

            _logger.Information("Finished uploading batches of type {EntityType}", entityType);
        }

        private async Task UpsertItemAsync(CosmosRegisteredEntity entity, PartitionKey partitionKey, CancellationToken cancellationToken)
        {
            var attempt = 0;
            Exception lastException = null;
            while (attempt < 3)
            {
                try
                {
                    await _container.UpsertItemAsync(entity, partitionKey, cancellationToken: cancellationToken);
                    return;
                }
                catch (CosmosException ex)
                {
                    lastException = ex;
                    if ((int) ex.StatusCode == 408)
                    {
                        _logger.Warning($"Attempt {attempt + 1} of {entity} timed out");
                    }
                }

                attempt++;
            }

            throw new Exception($"Failed to upload after 3 attempts. Last exception: {lastException?.Message}", lastException);
        }

        private long SizeOf(RegisteredEntity item)
        {
            var json = JsonConvert.SerializeObject(item);
            return json.Length;
        }
    }
}