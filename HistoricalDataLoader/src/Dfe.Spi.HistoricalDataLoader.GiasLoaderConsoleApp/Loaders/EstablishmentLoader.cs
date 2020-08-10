using Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp.Models;
using Newtonsoft.Json;
using Serilog;

namespace Dfe.Spi.HistoricalDataLoader.GiasLoaderConsoleApp.Loaders
{
    internal class EstablishmentLoader : LoaderBase<Establishment, long, EstablishmentEntity>
    {
        public EstablishmentLoader(string dataDirectory, string storageConnectionString, ILogger logger)
            : base(dataDirectory, storageConnectionString, "establishments", "establishments", "establishment", logger)
        {
        }

        protected override EstablishmentEntity ConvertModelToEntity(Establishment model)
        {
            return new EstablishmentEntity
            {
                Establishment = JsonConvert.SerializeObject(model),
            };
        }
    }
    
    internal class EstablishmentEntity : LoaderTableEntityBase
    {
        public string Establishment
        {
            get;
            set;
        }
        
    }
}