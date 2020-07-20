using Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp.Models;
using Newtonsoft.Json;
using Serilog;

namespace Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp.Loaders
{
    internal class LocalAuthorityLoader : LoaderBase<LocalAuthority, long, LocalAuthorityEntity>
    {
        public LocalAuthorityLoader(string dataDirectory, string storageConnectionString, ILogger logger)
            : base(dataDirectory, storageConnectionString, "localauthorities", "local-authorities", "localauthority", logger)
        {
        }

        protected override LocalAuthorityEntity ConvertModelToEntity(LocalAuthority model)
        {
            return new LocalAuthorityEntity
            {
                LocalAuthority = JsonConvert.SerializeObject(model),
            };
        }
    }

    internal class LocalAuthorityEntity : LoaderTableEntityBase
    {
        public string LocalAuthority
        {
            get;
            set;
        }
    }
}