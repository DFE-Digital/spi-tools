using System;

namespace Dfe.Spi.HistoricalDataLoader.RegistryLoaderConsoleApp.Models
{
    public class LinkedEntity : Entity
    {
        public string LinkType { get; set; }

        public string LinkedBy { get; set; }

        public string LinkedReason { get; set; }
        
        public DateTime? LinkedAt { get; set; }
    }
}