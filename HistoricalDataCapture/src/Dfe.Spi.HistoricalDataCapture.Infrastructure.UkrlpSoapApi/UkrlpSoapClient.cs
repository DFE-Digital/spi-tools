using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dfe.Spi.HistoricalDataCapture.Domain.Configuration;
using Dfe.Spi.HistoricalDataCapture.Domain.UkrlpClient;
using RestSharp;

namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.UkrlpSoapApi
{
    public class UkrlpSoapClient : IUkrlpClient
    {
        private readonly IRestClient _restClient;
        private readonly IUkrlpSoapMessageBuilder _messageBuilder;

        internal UkrlpSoapClient(
            IRestClient restClient, 
            IUkrlpSoapMessageBuilder messageBuilder,
            UkrlpConfiguration configuration)
        {
            _restClient = restClient;
            _restClient.BaseUrl = new Uri(configuration.SoapUrl, UriKind.Absolute);
            _messageBuilder = messageBuilder;
        }

        public UkrlpSoapClient(
            IRestClient restClient, 
            UkrlpConfiguration configuration)
            : this(restClient, new UkrlpSoapMessageBuilder(configuration.StakeholderId), configuration)
        {
        }
        
        public async Task<byte[]> GetChangesSinceAsync(DateTime sinceTime, CancellationToken cancellationToken)
        {
            var message = _messageBuilder.BuildMessageToGetUpdatesSince(sinceTime);

            var request = new RestRequest(Method.POST);
            request.AddParameter("text/xml", message, ParameterType.RequestBody);
            request.AddHeader("SOAPAction", "retrieveAllProviders");

            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            var result = EnsureSuccessResponseAndExtractResult(response);

            return Encoding.UTF8.GetBytes(result.ToString());
        }
        
        private static XElement EnsureSuccessResponseAndExtractResult(IRestResponse response)
        {
            XDocument document;
            try
            {
                document = XDocument.Parse(response.Content);
            }
            catch (Exception ex)
            {
                throw new UkrlpSoapApiException($"Error deserializing SOAP response: {ex.Message} (response: {response.Content})", ex);
            }
            
            var envelope = document.Elements().Single();
            var body = envelope.GetElementByLocalName("Body");

            if (!response.IsSuccessful)
            {
                var fault = body.Elements().Single();
                var faultCode = fault.GetElementByLocalName("faultcode");
                var faultString = fault.GetElementByLocalName("faultstring");
                throw new SoapException(faultCode.Value, faultString.Value);
            }

            return body.Elements().First();
        }
    }
}