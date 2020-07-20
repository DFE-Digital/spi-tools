using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Registry;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Registry
{
    public class FileSystemPreparedRegistryRepository : IPreparedRegistryRepository
    {
        private string _dataDirectory;
        private RegistryIndex _index;

        public FileSystemPreparedRegistryRepository(string dataDirectory)
        {
            _dataDirectory = Path.Combine(dataDirectory, "registry");

            _index = new RegistryIndex(_dataDirectory);
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            await _index.InitAsync(cancellationToken);
        }
        
        public async Task<RegisteredEntity> GetRegisteredEntityAsync(string type, string sourceSystemName, string sourceSystemId, DateTime date, CancellationToken cancellationToken)
        {
            var id = _index.GetRegisteredEntityId(type, sourceSystemName, sourceSystemId, date);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var json = await FileHelper.ReadFileAsStringAsync(Path.Combine(_dataDirectory, $"{id}.json"));
            return JsonConvert.DeserializeObject<RegisteredEntity>(json);
        }

        public async Task StoreRegisteredEntity(RegisteredEntity entity, DateTime date, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(entity);
            await FileHelper.WriteStringToFileAsync(Path.Combine(_dataDirectory, $"{entity.Id}.json"), json);

            await _index.AddRegisteredEntityIdAsync(entity, entity.ValidFrom, cancellationToken);
        }

        public async Task DeleteRegisteredEntity(string id, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(Path.Combine(_dataDirectory, $"{id}.json"));
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            await _index.DeleteRegisteredEntityIdAsync(id, cancellationToken);
        }
    }
}