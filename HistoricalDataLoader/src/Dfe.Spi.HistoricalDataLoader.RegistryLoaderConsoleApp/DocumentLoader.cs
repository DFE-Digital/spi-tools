using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
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

        internal async Task LoadAsync(CancellationToken cancellationToken)
        {
            var index = await LoadIndexAsync();

            var entityTypes = index.Keys
                .Where(x=>x.Equals("management-group", StringComparison.InvariantCultureIgnoreCase)) //TODO: Remove
                .OrderBy(x => x)
                .ToArray();
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
            var partitionedEntities = new Dictionary<string, List<CosmosRegisteredEntity>>();

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
                    partitionedEntities.Add(registeredEntity.PartitionableId, new List<CosmosRegisteredEntity>());
                }

                partitionedEntities[registeredEntity.PartitionableId].Add(registeredEntity);
            }

            _logger.Information("Built {NumberOfBatches} batches of type {EntityType}", partitionedEntities.Count, entityType);

            // Upload
             const long maxRequestSize = 700000;
            
            _logger.Information("Starting to uploaded batches of type {EntityType}", entityType);
            var partitionIds = partitionedEntities.Keys.ToArray();
            for (var i = 0; i < partitionIds.Length; i++)
            {
                var partitionId = partitionIds[i];
                _logger.Debug("Uploading partition {Index} ({PartitionId}) of {TotalNumberOfPartitions} of type {EntityType}",
                    i, partitionId, partitionIds.Length, entityType);
            
                var registeredEntities = partitionedEntities[partitionId];
                var partitionKey = new PartitionKey(partitionId);
            
                var potentialBatchSize = registeredEntities
                    .Select(SizeOf)
                    .Sum();
                if (potentialBatchSize >= maxRequestSize)
                {
                    _logger.Information("Batch would be too large, processing individually");
                    for (var j = 0; j < registeredEntities.Count; j++)
                    {
                        _logger.Debug("Uploading document {DocumentIndex} of {NumberOfDocuments} from partition {Index} ({PartitionId})",
                            j, registeredEntities.Capacity, i, partitionId);
                        await _container.UpsertItemAsync(registeredEntities[j], partitionKey, cancellationToken: cancellationToken);
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

        private long SizeOf(RegisteredEntity item)
        {
            var json = JsonConvert.SerializeObject(item);
            return json.Length;
        }
    }
}