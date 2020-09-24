using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.FileSystem.Ukrlp
{
    public class FileSystemPreparedUkrlpRepository : IPreparedUkrlpRepository
    {
        private readonly string _dataDirectory;
        private readonly VersionIndex<long> _providerIndex;

        public FileSystemPreparedUkrlpRepository(string dataDirectory)
        {
            _dataDirectory = Path.Combine(dataDirectory, "ukrlp");
            
            _providerIndex = new VersionIndex<long>(_dataDirectory, "providers-index");
        }
        
        public async Task InitAsync(CancellationToken cancellationToken)
        {
            await _providerIndex.InitAsync(cancellationToken);
        }
        
        
        public async Task<Provider> GetProviderAsync(long ukprn, DateTime date, CancellationToken cancellationToken)
        {
            var dateOfPreviousVersion = _providerIndex.GetDateOfMostRecentVersionBefore(ukprn, date, cancellationToken);
            if (!dateOfPreviousVersion.HasValue)
            {
                return null;
            }

            var fileName = $"provider-{ukprn}-{dateOfPreviousVersion.Value:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);
            var json = await FileHelper.ReadFileAsStringAsync(path);
            
            return JsonConvert.DeserializeObject<Provider>(json);
        }

        public async Task StoreProviderAsync(Provider provider, DateTime date, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(provider);
            var fileName = $"provider-{provider.UnitedKingdomProviderReferenceNumber}-{date:yyyyMMdd}.json";
            var path = Path.Combine(_dataDirectory, fileName);

            await FileHelper.WriteStringToFileAsync(path, json);

            _providerIndex.AddDateToIndex(provider.UnitedKingdomProviderReferenceNumber, date);
        }

        public async Task FlushAsync()
        {
            await _providerIndex.FlushAsync();
        }
    }
}