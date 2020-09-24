using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Gias
{
    public class FileSystemPreparedGiasRepository : IPreparedGiasRepository
    {
        private readonly string _dataDirectory;
        private readonly VersionIndex<long> _establishmentIndex;
        private readonly VersionIndex<long> _groupIndex;
        private readonly VersionIndex<int> _localAuthorityIndex;

        public FileSystemPreparedGiasRepository(string dataDirectory)
        {
            _dataDirectory = Path.Combine(dataDirectory, "gias");
            
            _establishmentIndex = new VersionIndex<long>(_dataDirectory, "establishments-index");
            _groupIndex = new VersionIndex<long>(_dataDirectory, "groups-index");
            _localAuthorityIndex = new VersionIndex<int>(_dataDirectory, "local-authorities-index");
        }
        
        public async Task InitAsync(CancellationToken cancellationToken)
        {
            await _establishmentIndex.InitAsync(cancellationToken);
            await _groupIndex.InitAsync(cancellationToken);
            await _localAuthorityIndex.InitAsync(cancellationToken);
        }
        
        
        public async Task<Establishment> GetEstablishmentAsync(long urn, DateTime date, CancellationToken cancellationToken)
        {
            var dateOfPreviousVersion = _establishmentIndex.GetDateOfMostRecentVersionBefore(urn, date, cancellationToken);
            if (!dateOfPreviousVersion.HasValue)
            {
                return null;
            }

            var fileName = $"establishment-{urn}-{dateOfPreviousVersion.Value:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);
            var json = await FileHelper.ReadFileAsStringAsync(path);
            
            return JsonConvert.DeserializeObject<Establishment>(json);
        }

        public async Task StoreEstablishmentAsync(Establishment establishment, DateTime date, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(establishment);
            var fileName = $"establishment-{establishment.Urn}-{date:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);

            await FileHelper.WriteStringToFileAsync(path, json);

            _establishmentIndex.AddDateToIndex(establishment.Urn, date);
        }
        

        public async Task<Group> GetGroupAsync(long uid, DateTime date, CancellationToken cancellationToken)
        {
            var dateOfPreviousVersion = _groupIndex.GetDateOfMostRecentVersionBefore(uid, date, cancellationToken);
            if (!dateOfPreviousVersion.HasValue)
            {
                return null;
            }

            var fileName = $"group-{uid}-{dateOfPreviousVersion.Value:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);
            var json = await FileHelper.ReadFileAsStringAsync(path);
            
            return JsonConvert.DeserializeObject<Group>(json);
        }

        public async Task StoreGroupAsync(Group group, DateTime date, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(group);
            var fileName = $"group-{group.Uid}-{date:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);

            await FileHelper.WriteStringToFileAsync(path, json);

            _groupIndex.AddDateToIndex(group.Uid, date);
        }
        

        public async Task<LocalAuthority> GetLocalAuthorityAsync(int laCode, DateTime date, CancellationToken cancellationToken)
        {
            var dateOfPreviousVersion = _localAuthorityIndex.GetDateOfMostRecentVersionBefore(laCode, date, cancellationToken);
            if (!dateOfPreviousVersion.HasValue)
            {
                return null;
            }

            var fileName = $"localauthority-{laCode}-{dateOfPreviousVersion.Value:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);
            var json = await FileHelper.ReadFileAsStringAsync(path);
            
            return JsonConvert.DeserializeObject<LocalAuthority>(json);
        }

        public async Task StoreLocalAuthorityAsync(LocalAuthority localAuthority, DateTime date, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(localAuthority);
            var fileName = $"localauthority-{localAuthority.Code}-{date:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);

            await FileHelper.WriteStringToFileAsync(path, json);

            _localAuthorityIndex.AddDateToIndex(localAuthority.Code, date);
        }

        public async Task FlushAsync()
        {
            await _establishmentIndex.FlushAsync();
            await _groupIndex.FlushAsync();
            await _localAuthorityIndex.FlushAsync();
        }
    }
}