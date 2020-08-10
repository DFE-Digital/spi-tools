using System;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp
{
    public class Provider
    {
        public long UnitedKingdomProviderReferenceNumber { get; set; }
        public string ProviderName { get; set; }
        public string AccessibleProviderName { get; set; }
        public ProviderContact[] ProviderContacts { get; set; }
        public DateTime? ProviderVerificationDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string ProviderStatus { get; set; }
        public VerificationDetails[] Verifications { get; set; }
    }
}