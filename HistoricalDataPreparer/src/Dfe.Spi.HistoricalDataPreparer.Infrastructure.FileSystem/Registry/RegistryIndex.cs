using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Registry;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Registry
{
    internal class RegistryIndex
    {
        private Dictionary<string, Dictionary<DateTime, string>> _sourceEntityIndex;
        private Dictionary<string, List<string>> _entityTypeIndex;
        private string _sourceEntityIndexPath;
        private string _entityTypeIndexPath;

        public RegistryIndex(string dataDirectory)
        {
            _sourceEntityIndexPath = Path.Combine(dataDirectory, "source-entity-index.json");
            _entityTypeIndexPath = Path.Combine(dataDirectory, "entity-type-index.json");
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(_sourceEntityIndexPath))
            {
                var json = await FileHelper.ReadFileAsStringAsync(_sourceEntityIndexPath);
                _sourceEntityIndex = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<DateTime, string>>>(json);
            }
            else
            {
                _sourceEntityIndex = new Dictionary<string, Dictionary<DateTime, string>>();
            }

            if (File.Exists(_entityTypeIndexPath))
            {
                var json = await FileHelper.ReadFileAsStringAsync(_entityTypeIndexPath);
                _entityTypeIndex = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            }
            else
            {
                _entityTypeIndex = new Dictionary<string, List<string>>();
            }
        }

        public string GetRegisteredEntityId(string entityType, string sourceSystemName, string sourceSystemId, DateTime date)
        {
            var key = $"{entityType}:{sourceSystemName}:{sourceSystemId}".ToLower();
            if (!_sourceEntityIndex.ContainsKey(key))
            {
                return null;
            }

            var mostRecentDateBeforeRequested = _sourceEntityIndex[key]
                .Keys.ToArray()
                // .Where(x => x <= date) // Removed this as it might be causing issue, but data is processed in date order, so the most recent date will be the one we want
                .OrderByDescending(x => x)
                .First();
            return _sourceEntityIndex[key][mostRecentDateBeforeRequested];
        }

        public void AddRegisteredEntityId(RegisteredEntity registeredEntity, DateTime date)
        {
            AddToSourceEntityIndex(registeredEntity, date);

            AddToEntityTypeIndex(registeredEntity);
        }

        public void DeleteRegisteredEntityId(string id)
        {
            DeleteFromSourceEntityIndex(id);
            
            DeleteFromEntityTypeIndex(id);
        }

        public async Task FlushAsync()
        {
            var sourceEntityIndexJson = JsonConvert.SerializeObject(_sourceEntityIndex);
            await FileHelper.WriteStringToFileAsync(_sourceEntityIndexPath, sourceEntityIndexJson);

            var entityTypeIndexJson = JsonConvert.SerializeObject(_entityTypeIndex);
            await FileHelper.WriteStringToFileAsync(_entityTypeIndexPath, entityTypeIndexJson);
        }


        private void AddToSourceEntityIndex(RegisteredEntity registeredEntity, DateTime date)
        {
            foreach (var entity in registeredEntity.Entities)
            {
                var key = $"{entity.EntityType}:{entity.SourceSystemName}:{entity.SourceSystemId}".ToLower();
                if (!_sourceEntityIndex.ContainsKey(key))
                {
                    _sourceEntityIndex.Add(key, new Dictionary<DateTime, string>());
                }

                var entityVersions = _sourceEntityIndex[key];
                if (entityVersions.ContainsKey(date))
                {
                    entityVersions.Remove(date);
                }

                entityVersions.Add(date, registeredEntity.Id);
            }
        }

        private void AddToEntityTypeIndex(RegisteredEntity registeredEntity)
        {
            if (!_entityTypeIndex.ContainsKey(registeredEntity.Type))
            {
                _entityTypeIndex.Add(registeredEntity.Type, new List<string>());
            }

            _entityTypeIndex[registeredEntity.Type].Add(registeredEntity.Id);
        }

        private void DeleteFromSourceEntityIndex(string id)
        {
            var toDelete = new List<KeyValuePair<string, DateTime>>();

            foreach (var entityKey in _sourceEntityIndex.Keys)
            {
                foreach (var versionDate in _sourceEntityIndex[entityKey].Keys)
                {
                    if (_sourceEntityIndex[entityKey][versionDate].Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    {
                        toDelete.Add(new KeyValuePair<string, DateTime>(entityKey, versionDate));
                    }
                }
            }

            foreach (var keyValuePair in toDelete)
            {
                var entityVersions = _sourceEntityIndex[keyValuePair.Key];

                entityVersions.Remove(keyValuePair.Value);

                if (entityVersions.Count == 0)
                {
                    _sourceEntityIndex.Remove(keyValuePair.Key);
                }
            }
        }

        private void DeleteFromEntityTypeIndex(string id)
        {
            var keys = _entityTypeIndex.Keys.ToArray();

            foreach (var key in keys)
            {
                _entityTypeIndex[key].Remove(id);
                if (_entityTypeIndex[key].Count == 0)
                {
                    _entityTypeIndex.Remove(key);
                }
            }
        }

        private async Task SaveAsync()
        {
            var sourceEntityIndexJson = JsonConvert.SerializeObject(_sourceEntityIndex);
            await FileHelper.WriteStringToFileAsync(_sourceEntityIndexPath, sourceEntityIndexJson);

            var entityTypeIndexJson = JsonConvert.SerializeObject(_entityTypeIndex);
            await FileHelper.WriteStringToFileAsync(_entityTypeIndexPath, entityTypeIndexJson);
        }
    }
}