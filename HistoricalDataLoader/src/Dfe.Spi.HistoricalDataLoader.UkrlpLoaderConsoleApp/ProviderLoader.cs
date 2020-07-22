using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataLoader.Common;
using Dfe.Spi.HistoricalDataLoader.UkrlpLoaderConsoleApp.Models;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Serilog;

namespace Dfe.Spi.HistoricalDataLoader.UkrlpLoaderConsoleApp
{
    public class ProviderLoader
    {
        private readonly string _entityType = nameof(Provider);

        private readonly string _dataDirectory;
        private readonly string _storageConnectionString;
        private readonly string _storageTableName;
        private readonly string _indexFilePrefix;
        private readonly string _dataFilePrefix;
        private readonly ILogger _logger;

        public ProviderLoader(
            string dataDirectory,
            string storageConnectionString,
            ILogger logger)
        {
            _dataDirectory = dataDirectory;
            _storageConnectionString = storageConnectionString;
            _storageTableName = "providers";
            _indexFilePrefix = "providers";
            _dataFilePrefix = "provider";
            _logger = logger;
        }

        internal async Task LoadAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Loading {EntityType} index...", _entityType);
            var index = await LoadIndexAsync();
            
            _logger.Information("Ensuring table exists...");
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(_storageTableName);
            await table.CreateIfNotExistsAsync(cancellationToken);

            var identifiers = index.Keys.ToArray();
            for (var i = 0; i < identifiers.Length; i++)
            {
                var identifier = identifiers[i];
                var versionDates = index[identifier];
                _logger.Information("{EntityType} {Index} of {RecordCount}: Loading {Identifier} with {NumberOfVersions} versions",
                    _entityType, i + 1, identifiers.Length, identifier, versionDates.Length);

                var batchOperation = new TableBatchOperation();
                for (var j = 0; j < versionDates.Length; j++)
                {
                    var versionDate = versionDates[j];
                    var isCurrent = j == versionDates.Length - 1;
                    
                    var model = await LoadVersionAsync(identifier, versionDate);
                    model.IsCurrent = isCurrent;
                    model.PointInTime = versionDate;
                    
                    var entity = ConvertModelToEntity(model);
                    entity.IsCurrent = isCurrent;
                    entity.PointInTime = versionDate;
                    entity.PartitionKey = identifier.ToString();
                    entity.RowKey = versionDate.ToString("yyyyMMdd");
                    batchOperation.InsertOrReplace(entity);

                    if (isCurrent)
                    {
                        var currentEntity = ConvertModelToEntity(model);
                        currentEntity.IsCurrent = true;
                        currentEntity.PointInTime = versionDate;
                        currentEntity.PartitionKey = identifier.ToString();
                        currentEntity.RowKey = "current";
                        batchOperation.InsertOrReplace(currentEntity);
                    }
                    
                }

                await table.ExecuteBatchAsync(batchOperation, cancellationToken);
            }
        }
        
        private ProviderEntity ConvertModelToEntity(Provider model)
        {
            return new ProviderEntity
            {
                ProviderJson = JsonConvert.SerializeObject(model),
            };
        }

        protected async Task<Dictionary<long, DateTime[]>> LoadIndexAsync()
        {
            var path = Path.Combine(_dataDirectory, $"{_indexFilePrefix}-index.json");
            return await FileSystemHelper.ReadFileAsAsync<Dictionary<long, DateTime[]>>(path);
        }

        protected async Task<Provider> LoadVersionAsync(long identifier, DateTime pointInTime)
        {
            var path = Path.Combine(_dataDirectory, $"{_dataFilePrefix}-{identifier}-{pointInTime:yyyyMMdd}.json");
            return await FileSystemHelper.ReadFileAsAsync<Provider>(path);
        }
    }
    
    internal class ProviderEntity : TableEntity
    {
        public string ProviderJson { get; set; }
        public DateTime PointInTime { get; set; }
        public bool IsCurrent { get; set; }
    }
}