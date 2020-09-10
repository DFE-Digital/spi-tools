using System;
using System.IO;
using System.Linq;
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
            var storableEntity = Map(entity);
            
            var json = JsonConvert.SerializeObject(storableEntity);
            await FileHelper.WriteStringToFileAsync(Path.Combine(_dataDirectory, $"{storableEntity.Id}.json"), json);

            await _index.AddRegisteredEntityIdAsync(storableEntity, storableEntity.ValidFrom, cancellationToken);
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
        
        
        
        private CosmosRegisteredEntity Map(RegisteredEntity registeredEntity)
        {
            var partitionableId = registeredEntity.Entities.FirstOrDefault(e => e.Urn.HasValue)?.Urn?.ToString();
            if (string.IsNullOrEmpty(partitionableId))
            {
                partitionableId = registeredEntity.Entities.FirstOrDefault(e => e.Ukprn.HasValue)?.Ukprn?.ToString();
            }

            if (string.IsNullOrEmpty(partitionableId))
            {
                partitionableId = registeredEntity.Entities.FirstOrDefault(e => !string.IsNullOrEmpty(e.ManagementGroupCode))?.ManagementGroupCode;
            }

            return new CosmosRegisteredEntity
            {
                Id = registeredEntity.Id,
                Type = registeredEntity.Type,
                ValidFrom = registeredEntity.ValidFrom,
                ValidTo = registeredEntity.ValidTo,
                Entities = registeredEntity.Entities,
                Links = registeredEntity.Links,

                PartitionableId = partitionableId,
                SearchableSourceSystemIdentifiers = registeredEntity.GetSearchableValues(e => $"{e.SourceSystemName}:{e.SourceSystemId}"),
                SearchableName = registeredEntity.GetSearchableValues(e => e.Name),
                SearchableType = registeredEntity.GetSearchableValues(e => e.Type),
                SearchableSubType = registeredEntity.GetSearchableValues(e => e.SubType),
                SearchableStatus = registeredEntity.GetSearchableValues(e => e.Status),
                SearchableOpenDate = registeredEntity.GetSearchableValues(e => e.OpenDate),
                SearchableCloseDate = registeredEntity.GetSearchableValues(e => e.CloseDate),
                SearchableUrn = registeredEntity.GetSearchableValues(e => e.Urn),
                SearchableUkprn = registeredEntity.GetSearchableValues(e => e.Ukprn),
                SearchableUprn = registeredEntity.GetSearchableValues(e => e.Uprn),
                SearchableCompaniesHouseNumber = registeredEntity.GetSearchableValues(e => e.CompaniesHouseNumber),
                SearchableCharitiesCommissionNumber = registeredEntity.GetSearchableValues(e => e.CharitiesCommissionNumber),
                SearchableAcademyTrustCode = registeredEntity.GetSearchableValues(e => e.AcademyTrustCode),
                SearchableDfeNumber = registeredEntity.GetSearchableValues(e => e.DfeNumber),
                SearchableLocalAuthorityCode = registeredEntity.GetSearchableValues(e => e.LocalAuthorityCode),
                SearchableManagementGroupType = registeredEntity.GetSearchableValues(e => e.ManagementGroupType),
                SearchableManagementGroupId = registeredEntity.GetSearchableValues(e => e.ManagementGroupId),
                SearchableManagementGroupCode = registeredEntity.GetSearchableValues(e => e.ManagementGroupCode),
                SearchableManagementGroupUkprn = registeredEntity.GetSearchableValues(e => e.ManagementGroupUkprn),
                SearchableManagementGroupCompaniesHouseNumber = registeredEntity.GetSearchableValues(e => e.ManagementGroupCompaniesHouseNumber),
            };
        }
    }
}