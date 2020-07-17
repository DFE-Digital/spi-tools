using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;
using Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage.Gias.CsvParsing;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage.Gias
{
    public class BlobGiasHistoricalRepository : AzureBlobStorageRepository, IGiasHistoricalRepository
    {
        public BlobGiasHistoricalRepository(string connectionString)
            : base(connectionString, "gias")
        {
        }

        public async Task<GiasDayData> GetDayDataAsync(DateTime date, CancellationToken cancellationToken)
        {
            var blobs = await ListBlobsAsync(date.ToString("yyyy-MM-dd"), cancellationToken);
            if (blobs.Length == 0)
            {
                return new GiasDayData
                {
                    Establishments = new Establishment[0],
                    LocalAuthorities = new LocalAuthority[0],
                    Groups = new Group[0],
                    GroupLinks = new GroupLink[0],
                };
            }

            await using var blobStream = await DownloadAsync(blobs.FirstOrDefault(), cancellationToken);
            using var zip = new ZipArchive(blobStream);
            var establishments = await ReadEstablishments(zip);
            var localAuthorities = ExtractLocalAuthoritiesFromEstablishments(establishments);
            var groups = await ReadGroups(zip);
            var groupLinks = await ReadGroupLinks(zip);
            
            return new GiasDayData
            {
                Establishments = establishments,
                LocalAuthorities = localAuthorities,
                Groups = groups,
                GroupLinks = groupLinks,
            };
        }

        private async Task<Establishment[]> ReadEstablishments(ZipArchive zip)
        {
            return await ReadFromZip<Establishment, EstablishmentFileParser>(zip, "Eapim_Daily_Download.csv");
        }
        private async Task<Group[]> ReadGroups(ZipArchive zip)
        {
            return await ReadFromZip<Group, GroupFileParser>(zip, "groups.csv");
        }
        private async Task<GroupLink[]> ReadGroupLinks(ZipArchive zip)
        {
            return await ReadFromZip<GroupLink, GroupLinkFileParser>(zip, "groupLinks.csv");
        }
        private async Task<TItem[]> ReadFromZip<TItem, TParser>(ZipArchive zip, string entryName)
            where TParser : CsvFileParser<TItem>
        {
            var zipEntry = zip.Entries.SingleOrDefault(e => e.Name == entryName);
            if (zipEntry == null)
            {
                throw new FileNotFoundException($"Could not find {entryName} in zip");
            }

            await using var stream = zipEntry.Open();
            using var reader = new StreamReader(stream);
            using var parser = (TParser)Activator.CreateInstance(typeof(TParser), BindingFlags.Default, null, new[] {reader}, null);
            
            var records = parser.GetRecords();
            return records;
        }
        
        
        private LocalAuthority[] ExtractLocalAuthoritiesFromEstablishments(Establishment[] establishments)
        {
            return establishments
                .GroupBy(e => e.LA.Code)
                .Select(g => new LocalAuthority
                {
                    Code = int.Parse(g.Key),
                    Name = g.First().LA.DisplayName,
                })
                .ToArray();
        }
    }
}