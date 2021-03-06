using System;
using System.Linq;
using System.Xml.Linq;

namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.UkrlpSoapApi
{internal interface IUkrlpSoapMessageBuilder
    {
        string BuildMessageToGetSpecificUkprn(long ukprn);
        string BuildMessageToGetSpecificUkprns(long[] ukprns);
        string BuildMessageToGetUpdatesSince(DateTime updatedSince, string status);
    }

    internal class UkrlpSoapMessageBuilder : IUkrlpSoapMessageBuilder
    {
        protected static readonly XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        protected static readonly XNamespace ukrlpNs = "http://ukrlp.co.uk.server.ws.v3";

        private readonly string _stakeholderId;
        private int _queryId;

        public UkrlpSoapMessageBuilder(string stakeholderId)
        {
            _stakeholderId = stakeholderId;
            _queryId = 1;
        }

        public string BuildMessageToGetSpecificUkprn(long ukprn)
        {
            return BuildMessageToGetSpecificUkprns(new[] {ukprn});
        }

        public string BuildMessageToGetSpecificUkprns(long[] ukprns)
        {
            var selectionCriteria = new XElement("SelectionCriteria",
                new XElement("UnitedKingdomProviderReferenceNumberList",
                    ukprns.Select(ukprn => new XElement("UnitedKingdomProviderReferenceNumber", ukprn))),
                new XElement("CriteriaCondition", "OR"),
                new XElement("ApprovedProvidersOnly", "No"),
                new XElement("ProviderStatus", "A"));

            var envelope = BuildEnvelope(selectionCriteria);
            return envelope.ToString();
        }

        public string BuildMessageToGetUpdatesSince(DateTime updatedSince, string status)
        {
            var selectionCriteria = new XElement("SelectionCriteria",
                new XElement("ProviderUpdatedSince", updatedSince.ToUniversalTime().ToString("O")),
                new XElement("CriteriaCondition", "OR"),
                new XElement("ApprovedProvidersOnly", "No"),
                new XElement("ProviderStatus", status));

            var envelope = BuildEnvelope(selectionCriteria);
            return envelope.ToString();
        }

        private XElement BuildEnvelope(XElement selectionCriteria)
        {
            return new XElement(soapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", soapNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "ukrlp", ukrlpNs.NamespaceName),
                new XElement(soapNs + "Header"),
                new XElement(soapNs + "Body",
                    BuildRequest(selectionCriteria)));
        }

        private XElement BuildRequest(XElement selectionCriteria)
        {
            selectionCriteria.Add(new XElement("StakeholderId", _stakeholderId));

            return new XElement(ukrlpNs + "ProviderQueryRequest",
                selectionCriteria,
                new XElement("QueryId", _queryId++));
        }
    }
}