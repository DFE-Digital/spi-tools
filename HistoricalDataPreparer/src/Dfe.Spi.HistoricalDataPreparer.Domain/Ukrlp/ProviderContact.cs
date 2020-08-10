using System;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp
{
    public class ProviderContact
    {
        public string ContactType { get; set; }
        public AddressStructure ContactAddress { get; set; }
        public PersonNameStructure ContactPersonalDetails { get; set; }
        public string ContactRole { get; set; }
        public string ContactTelephone1 { get; set; }
        public string ContactTelephone2 { get; set; }
        public string ContactFax { get; set; }
        public string ContactWebsiteAddress { get; set; }
        public string ContactEmail { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}