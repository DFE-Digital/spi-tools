using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Gias
{
    public class VersionIndex<TKey>
    {
        private Dictionary<TKey, List<DateTime>> _index;
        private string _path;

        public VersionIndex(string dataDirectory, string indexName)
        {
            _path = Path.Combine(dataDirectory, $"{indexName}.json");
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_path))
            {
                _index = new Dictionary<TKey, List<DateTime>>();
                return;
            }

            var json = await FileHelper.ReadFileAsStringAsync(_path);
            _index = JsonConvert.DeserializeObject<Dictionary<TKey, List<DateTime>>>(json);
        }

        public DateTime? GetDateOfMostRecentVersionBefore(TKey key, DateTime date, CancellationToken cancellationToken)
        {
            if (!_index.ContainsKey(key))
            {
                return null;
            }

            var versions = _index[key];
            return versions
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }

        public async Task AddDateToIndexAsync(TKey key, DateTime date, CancellationToken cancellationToken)
        {
            if (!_index.ContainsKey(key))
            {
                _index.Add(key, new List<DateTime>());
            }

            var versions = _index[key];
            if (!versions.Any(x => x == date))
            {
                versions.Add(date);

                var json = JsonConvert.SerializeObject(_index);
                await FileHelper.WriteStringToFileAsync(_path, json);
            }
        }
    }
}