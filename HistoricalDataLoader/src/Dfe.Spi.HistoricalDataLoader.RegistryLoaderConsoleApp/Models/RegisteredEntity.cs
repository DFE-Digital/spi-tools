using System;

namespace Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp.Models
{
    public class RegisteredEntity
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public LinkedEntity[] Entities { get; set; }
        public Link[] Links { get; set; }
    }
}