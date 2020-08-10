using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.HistoricalDataPreparer.Domain.Translation;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.SpiApi.Translation
{
    public class SpiTranslationApi : ITranslation
    {
        private HttpClient _httpClient;
        private Dictionary<string, EnumMappings> _giasMappings;
        private Dictionary<string, EnumMappings> _ukrlpMappings;

        public SpiTranslationApi(string baseUrl, string subscriptionKey)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        }

        public async Task InitAsync(
            string oauthTokenEndpoint, 
            string oauthClientId, 
            string oauthClientSecret, 
            string oauthResource, 
            CancellationToken cancellationToken)
        {
            var bearerToken = await OAuthAuthenticator.GetBearerTokenAsync(oauthTokenEndpoint, oauthClientId, oauthClientSecret, oauthResource,
                cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            _giasMappings = await GetSystemEnums(SourceSystemNames.GetInformationAboutSchools, cancellationToken);
            _ukrlpMappings = await GetSystemEnums(SourceSystemNames.UkRegisterOfLearningProviders, cancellationToken);
        }
        
        public async Task<string> TranslateEnumValueAsync(string enumName, string sourceSystem, string sourceValue, CancellationToken cancellationToken)
        {
            Dictionary<string, EnumMappings> _mappings;

            if (sourceSystem.Equals(SourceSystemNames.GetInformationAboutSchools, StringComparison.InvariantCultureIgnoreCase))
            {
                _mappings = _giasMappings;
            }
            else if (sourceSystem.Equals(SourceSystemNames.UkRegisterOfLearningProviders, StringComparison.InvariantCultureIgnoreCase))
            {
                _mappings = _ukrlpMappings;
            }
            else
            {
                throw new Exception($"Unsupported system {sourceSystem} for getting translations");
            }

            var enumKey = _mappings.Keys.SingleOrDefault(k => k.Equals(enumName, StringComparison.InvariantCultureIgnoreCase));
            if (enumKey == null)
            {
                throw new Exception($"{sourceSystem} does not translate enum {enumName}");
            }

            var enumMappings = _mappings[enumKey].Mappings;
            foreach (var mapping in enumMappings)
            {
                if (mapping.Value.Any(v => v.Equals(sourceValue, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return mapping.Key;
                }
            }
            throw new Exception($"Enum {enumName} for {sourceSystem} does not have mapping for {sourceValue}");
        }
        
        private async Task<Dictionary<string, EnumMappings>> GetSystemEnums(string systemName, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"adapters/{systemName}/mappings", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error getting {systemName} translations: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, EnumMappings>>(json);
        }
    }

    internal class EnumMappings
    {
        public Dictionary<string, string[]> Mappings { get; set; }
    }
}