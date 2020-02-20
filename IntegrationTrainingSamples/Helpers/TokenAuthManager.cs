using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IntegrationTrainingSamples.Helpers
{
    class TokenAuthManager
    {
        private string _accessToken;
        private DateTime? _expires;

        private string _tokenUrl;
        private string _clientId;
        private string _clientSecret;
        private string _audience;

        public TokenAuthManager(string tokenUrl, string clientId, string clientSecret, string audience)
        {
            _tokenUrl = tokenUrl;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _audience = audience;
        }

        public async Task<string> GetBearerToken()
        {
            if (_accessToken == null || _expires == null || _expires <= DateTime.UtcNow.AddSeconds(-30))
            {
                _accessToken = null;

                Console.WriteLine("Retrieving token from authentication service...");
                using (HttpClient client = new HttpClient())
                {
                    var request = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("client_id", _clientId),
                        new KeyValuePair<string, string>("client_secret", _clientSecret),
                        new KeyValuePair<string, string>("audience", _audience),
                    });

                    var response = await client.PostAsync(_tokenUrl, request);
                    ProcessTokenResponse(response);
                }
            }
            else
            {
                Console.WriteLine("Existing token is still valid");
            }

            return _accessToken;
        }
        private async void ProcessTokenResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string reason = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error retreiving token: {(int)response.StatusCode} ({response.StatusCode.ToString()}): {reason}");
            }
            else
            {
                string contentJson = await response.Content.ReadAsStringAsync();
                dynamic content = JsonConvert.DeserializeObject(contentJson);

                _accessToken = ((dynamic)content).access_token.Value;
                var expirationSeconds = double.Parse(((dynamic)content).expires_in.Value.ToString());
                _expires = DateTime.UtcNow.AddSeconds(expirationSeconds);

                Console.WriteLine($"Received token expiring in {expirationSeconds} seconds at {_expires?.ToShortDateString()} {_expires?.ToShortTimeString()} UTC");
            }
        }
        public void ResetBearerToken()
        {
            _accessToken = null;
        }
    }
}