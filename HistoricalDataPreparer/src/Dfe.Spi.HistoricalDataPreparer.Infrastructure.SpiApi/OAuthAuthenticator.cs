using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Dfe.Spi.HistoricalDataPreparer.Infrastructure.SpiApi
{
    internal static class OAuthAuthenticator
    {
        internal static async Task<string> GetBearerTokenAsync(
            string tokenEndpoint, 
            string clientId, 
            string clientSecret, 
            string resource, 
            CancellationToken cancellationToken)
        {
            var token = await GetTokenFromEndpointAsync(tokenEndpoint, clientId, clientSecret, resource, cancellationToken);
            return token.AccessToken;
        }

        private static async Task<OAuthToken> GetTokenFromEndpointAsync(
            string tokenEndpoint, 
            string clientId, 
            string clientSecret, 
            string resource, 
            CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var body = "grant_type=client_credentials" +
                       $"&client_id={HttpUtility.UrlEncode(clientId)}" +
                       $"&client_secret={HttpUtility.UrlEncode(clientSecret)}" +
                       $"&resource={HttpUtility.UrlEncode(resource)}";
            var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new OAuthException($"Error calling token endpoint. Status code = {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OAuthToken>(json);
        }
    }

    internal class OAuthToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        
        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }
    }
}