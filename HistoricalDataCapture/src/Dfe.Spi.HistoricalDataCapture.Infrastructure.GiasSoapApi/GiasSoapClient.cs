using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dfe.Spi.HistoricalDataCapture.Domain.Configuration;
using Dfe.Spi.HistoricalDataCapture.Domain.GiasClient;
using Dfe.Spi.HistoricalDataCapture.Infrastructure.GiasSoapApi.Requests;
using RestSharp;

namespace Dfe.Spi.HistoricalDataCapture.Infrastructure.GiasSoapApi
{
    public class GiasSoapClient : IGiasClient
    {
        private readonly IRestClient _restClient;
        private readonly GiasConfiguration _configuration;
        private readonly IGiasSoapMessageBuilder<GetExtractRequest> _getExtractMessageBuilder;

        internal GiasSoapClient(
            IRestClient restClient,
            GiasConfiguration configuration,
            IGiasSoapMessageBuilder<GetExtractRequest> getExtractMessageBuilder)
        {
            _restClient = restClient;
            _configuration = configuration;
            _getExtractMessageBuilder = getExtractMessageBuilder;
        }
        public GiasSoapClient(
            GiasConfiguration configuration)
            : this(
                new RestClient(configuration.SoapUrl),
                configuration,
                new GetExtractMessageBuilder(configuration.SoapUsername, configuration.SoapPassword))
        {
        }
        
        public async Task<byte[]> DownloadExtractAsync(int extractId, CancellationToken cancellationToken)
        {
            var message = _getExtractMessageBuilder.Build(new GetExtractRequest
            {
                ExtractId = extractId,
            });

            var request = new RestRequest(Method.POST);
            request.AddParameter("text/xml", message, ParameterType.RequestBody);
            request.AddHeader("SOAPAction", "http://ws.edubase.texunatech.com/GetExtract");

            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            var soapResponse = EnsureSuccessResponseAndExtractResult(response);
            var zipAttachment = soapResponse.Attachments.FirstOrDefault();
            if (zipAttachment == null)
            {
                throw new Exception("Missing zip attachment");
            }

            return zipAttachment.Data;
        }
        
        private static SoapResponse EnsureSuccessResponseAndExtractResult(IRestResponse response)
        {
            ContentPart[] contentParts;
            ContentPart soapPart;
            XDocument document;
            try
            {
                contentParts = ParseResponseContent(response);
                soapPart = contentParts.SingleOrDefault(p =>
                    p.Headers["Content-Type"].StartsWith("application/xop+xml") ||
                    p.Headers["Content-Type"].StartsWith("text/xml"));
                if (soapPart == null)
                {
                    throw new Exception("Response does not appear to contain any soap content");
                }

                document = XDocument.Parse(Encoding.UTF8.GetString(soapPart.Data));
            }
            catch (Exception ex)
            {
                throw new GiasSoapApiException(
                    $"Error deserializing SOAP response: {ex.Message}", ex);
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

            return new SoapResponse
            {
                Result = body.Elements().First(),
                Attachments = contentParts.Where(p => p != soapPart).ToArray(),
            };
        }
        
        private static ContentPart[] ParseResponseContent(IRestResponse response)
        {
            if (response.ContentType.StartsWith("Multipart/Related"))
            {
                return ReadMultipart(response);
            }

            return new[]
            {
                new ContentPart
                {
                    Headers = new Dictionary<string, string>
                    {
                        {"Content-Type", response.ContentType}
                    },
                    Data = response.RawBytes,
                },
            };
        }
        
        private static ContentPart[] ReadMultipart(IRestResponse response)
        {
            // Get boundary
            var boundaryMatch =
                Regex.Match(response.ContentType, "boundary=\"([a-z0-9\\-=_\\.]{1,})\"", RegexOptions.IgnoreCase);
            if (!boundaryMatch.Success)
            {
                throw new Exception("Multipart response does not have boundary specified");
            }

            // Split content by boundary
            var boundary = Encoding.UTF8.GetBytes($"--{boundaryMatch.Groups[1].Value}");
            var index = 0;
            int lastIndex = -1;
            var parts = new List<byte[]>();
            var data = response.RawBytes;
            while ((index = IndexOf(data, boundary, index)) >= 0)
            {
                if (lastIndex > -1)
                {
                    var start = lastIndex + boundary.Length + 2;
                    var length = index - start - 2;
                    var partBuffer = new byte[length];
                    Array.Copy(data, start, partBuffer, 0, length);
                    parts.Add(partBuffer);
                }

                lastIndex = index;
                index += 1;
            }

            // Convert to content parts
            var splitter = Encoding.UTF8.GetBytes("\r\n\r\n");
            var contentParts = new ContentPart[parts.Count];
            for (var i = 0; i < parts.Count; i++)
            {
                var headerContentSplit = IndexOf(parts[i], splitter);

                var headersBuffer = new byte[headerContentSplit];
                Array.Copy(parts[i], headersBuffer, headersBuffer.Length);
                var headers = Encoding.UTF8.GetString(headersBuffer)
                    .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split(':'))
                    .ToDictionary(x => x[0].Trim(), x => x[1].Trim());

                var bodyBuffer = new byte[parts[i].Length - headerContentSplit - splitter.Length];
                Array.Copy(parts[i], headerContentSplit + splitter.Length, bodyBuffer, 0, bodyBuffer.Length);

                contentParts[i] = new ContentPart
                {
                    Headers = headers,
                    Data = bodyBuffer,
                };
            }

            // And breathe
            return contentParts;
        }
        
        private static int IndexOf(byte[] buffer, byte[] value, int startIndex = 0)
        {
            var index = startIndex;
            while (index + value.Length < buffer.Length)
            {
                var isMatch = true;
                for (var i = 0; i < value.Length && isMatch; i++)
                {
                    isMatch = buffer[index + i] == value[i];
                }

                if (isMatch)
                {
                    return index;
                }

                index += 1;
            }

            return -1;
        }


        private class SoapResponse
        {
            public XElement Result { get; set; }
            public ContentPart[] Attachments { get; set; }
        }

        private class ContentPart
        {
            public Dictionary<string, string> Headers { get; set; }
            public byte[] Data { get; set; }
        }
    }
}