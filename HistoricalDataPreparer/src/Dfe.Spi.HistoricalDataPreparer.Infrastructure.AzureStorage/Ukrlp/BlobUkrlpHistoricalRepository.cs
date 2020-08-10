using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure.Storage.Blobs.Models;
using Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.AzureStorage.Ukrlp
{
    public class BlobUkrlpHistoricalRepository : AzureBlobStorageRepository, IUkrlpHistoricalRepository
    {
        public BlobUkrlpHistoricalRepository(string connectionString) 
            : base(connectionString, "ukrlp")
        {
        }

        public async Task<UkrlpDayData> GetDayDataAsync(DateTime date, CancellationToken cancellationToken)
        {
            var blobs = await ListBlobsAsync(date.ToString("yyyy-MM-dd"), cancellationToken);
            var orderedBlobs = blobs.OrderBy(b => b.Name);

            var providers = new Dictionary<long, Provider>();
            foreach (var blob in orderedBlobs)
            {
                await ProcessDeltasAsync(blob, providers, cancellationToken);
            }

            return new UkrlpDayData
            {
                Providers = providers.Values.ToArray(),
            };
        }

        private async Task ProcessDeltasAsync(BlobItem blob, Dictionary<long, Provider> providers, CancellationToken cancellationToken)
        {
            await using var downloadStream = await DownloadAsync(blob, cancellationToken);
            var document = await XDocument.LoadAsync(downloadStream, LoadOptions.None, cancellationToken);
            var providersFromDocument = MapProvidersFromSoapResult(document.Root);

            foreach (var provider in providersFromDocument)
            {
                if (providers.ContainsKey(provider.UnitedKingdomProviderReferenceNumber))
                {
                    providers[provider.UnitedKingdomProviderReferenceNumber] = provider;
                }
                else
                {
                    providers.Add(provider.UnitedKingdomProviderReferenceNumber, provider);
                }
            }
        }
        
        private static Provider[] MapProvidersFromSoapResult(XElement result)
        {
            var matches = result.GetElementsByLocalName("MatchingProviderRecords");
            var providers = new Provider[matches.Length];

            for (var i = 0; i < matches.Length; i++)
            {
                var match = matches[i];
                
                providers[i] = new Provider
                {
                    UnitedKingdomProviderReferenceNumber = long.Parse(match.GetElementByLocalName("UnitedKingdomProviderReferenceNumber").Value),
                    ProviderName = match.GetElementByLocalName("ProviderName").Value,
                    AccessibleProviderName = match.GetElementByLocalName("AccessibleProviderName")?.Value,
                    ProviderStatus = match.GetElementByLocalName("ProviderStatus")?.Value,
                    ProviderVerificationDate = ReadNullableDateTime(match.GetElementByLocalName("ProviderVerificationDate")),
                    ExpiryDate = ReadNullableDateTime(match.GetElementByLocalName("ExpiryDate")),
                    ProviderContacts = MapProviderContactsFromSoapProvider(match),
                };

                var verifications = new List<VerificationDetails>();
                var verificationElements = match.GetElementsByLocalName("VerificationDetails");
                foreach (var verificationElement in verificationElements)
                {
                    verifications.Add(new VerificationDetails
                    {
                        Authority = verificationElement.GetElementByLocalName("VerificationAuthority").Value,
                        Id = verificationElement.GetElementByLocalName("VerificationID").Value,
                    });
                }
                providers[i].Verifications = verifications.ToArray();
            }

            return providers;
        }
        private static ProviderContact[] MapProviderContactsFromSoapProvider(XElement providerElement)
        {
            var contactElements = providerElement.GetElementsByLocalName("ProviderContact");
            if (contactElements == null || contactElements.Length == 0)
            {
                return new ProviderContact[0];
            }

            return contactElements.Select(contactElement => new ProviderContact
            {
                ContactType = contactElement.GetElementByLocalName("ContactType")?.Value,
                ContactRole = contactElement.GetElementByLocalName("ContactRole")?.Value,
                ContactTelephone1 = contactElement.GetElementByLocalName("ContactTelephone1")?.Value,
                ContactTelephone2 = contactElement.GetElementByLocalName("ContactTelephone2")?.Value,
                ContactFax = contactElement.GetElementByLocalName("ContactFax")?.Value,
                ContactWebsiteAddress = contactElement.GetElementByLocalName("ContactWebsiteAddress")?.Value,
                ContactEmail = contactElement.GetElementByLocalName("ContactEmail")?.Value,
                LastUpdated = ReadNullableDateTime(contactElement.GetElementByLocalName("LastUpdated")),
                ContactAddress = MapContactAddressFromSoapContactElement(contactElement),
                ContactPersonalDetails = MapPersonNameStructureFromSoapContactElement(contactElement),
            }).ToArray();
        }
        private static AddressStructure MapContactAddressFromSoapContactElement(XElement contactElement)
        {
            var addressElement = contactElement.GetElementByLocalName("ContactAddress");
            return addressElement == null
                ? null
                : new AddressStructure
                {
                    Address1 = addressElement.GetElementByLocalName("Address1")?.Value,
                    Address2 = addressElement.GetElementByLocalName("Address2")?.Value,
                    Address3 = addressElement.GetElementByLocalName("Address3")?.Value,
                    Address4 = addressElement.GetElementByLocalName("Address4")?.Value,
                    Town = addressElement.GetElementByLocalName("Town")?.Value,
                    County = addressElement.GetElementByLocalName("County")?.Value,
                    PostCode = addressElement.GetElementByLocalName("PostCode")?.Value,
                };
        }
        private static PersonNameStructure MapPersonNameStructureFromSoapContactElement(XElement contactElement)
        {
            var personalDetailsElement = contactElement.GetElementByLocalName("ContactPersonalDetails");
            return personalDetailsElement == null
                ? null
                : new PersonNameStructure
                {
                    PersonNameTitle = personalDetailsElement.GetElementByLocalName("PersonNameTitle")?.Value,
                    PersonGivenName = personalDetailsElement.GetElementByLocalName("PersonGivenName")?.Value,
                    PersonFamilyName = personalDetailsElement.GetElementByLocalName("PersonFamilyName")?.Value,
                    PersonNameSuffix = personalDetailsElement.GetElementByLocalName("PersonNameSuffix")?.Value,
                    PersonRequestedName = personalDetailsElement.GetElementByLocalName("PersonRequestedName")?.Value,
                };
        }
        private static DateTime? ReadNullableDateTime(XElement element)
        {
            if (element == null)
            {
                return null;
            }

            return DateTime.Parse(element.Value);
        }
    }
}