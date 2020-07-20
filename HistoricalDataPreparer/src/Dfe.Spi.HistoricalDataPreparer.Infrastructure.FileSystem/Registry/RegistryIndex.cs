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
        private Dictionary<string, Dictionary<DateTime, string>> _index;
        private string _path;
        
        public RegistryIndex(string dataDirectory)
        {
            _path = Path.Combine(dataDirectory, "index.json");
        }
        
        public async Task InitAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_path))
            {
                _index = new Dictionary<string, Dictionary<DateTime, string>>();
                return;
            }

            var json = await FileHelper.ReadFileAsStringAsync(_path);
            _index = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<DateTime, string>>>(json);
        }
        
        public string GetRegisteredEntityId(string entityType, string sourceSystemName, string sourceSystemId, DateTime date)
        {
            var key = $"{entityType}:{sourceSystemName}:{sourceSystemId}".ToLower();
            if (!_index.ContainsKey(key))
            {
                return null;
            }

            var mostRecentDateBeforeRequested = _index[key]
                .Keys.ToArray()
                .Where(x => x <= date)
                .OrderByDescending(x => x)
                .First();
            return _index[key][mostRecentDateBeforeRequested];
        }

        public async Task AddRegisteredEntityIdAsync(RegisteredEntity registeredEntity, DateTime date, CancellationToken cancellationToken)
        {
            foreach (var entity in registeredEntity.Entities)
            {
                var key = $"{entity.EntityType}:{entity.SourceSystemName}:{entity.SourceSystemId}".ToLower();
                if (!_index.ContainsKey(key))
                {
                    _index.Add(key, new Dictionary<DateTime, string>());
                }

                var entityVersions = _index[key];
                if (entityVersions.ContainsKey(date))
                {
                    entityVersions.Remove(date);
                }
                entityVersions.Add(date, registeredEntity.Id);
            }

            await SaveAsync();
        }

        public async Task DeleteRegisteredEntityIdAsync(string id, CancellationToken cancellationToken)
        {
            var toDelete = new List<KeyValuePair<string, DateTime>>();

            foreach (var entityKey in _index.Keys)
            {
                foreach (var versionDate in _index[entityKey].Keys)
                {
                    if (_index[entityKey][versionDate].Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    {
                        toDelete.Add(new KeyValuePair<string, DateTime>(entityKey, versionDate));
                    }
                }
            }

            foreach (var keyValuePair in toDelete)
            {
                var entityVersions = _index[keyValuePair.Key];

                entityVersions.Remove(keyValuePair.Value);

                if (entityVersions.Count == 0)
                {
                    _index.Remove(keyValuePair.Key);
                }
            }

            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            var json = JsonConvert.SerializeObject(_index);
            await FileHelper.WriteStringToFileAsync(_path, json);
        }
    }
}