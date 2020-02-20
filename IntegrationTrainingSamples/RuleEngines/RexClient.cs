using IntegrationTrainingSamples.Helpers;
using IntegrationTrainingSamples.Model;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTrainingSamples.RuleEngines
{
    public class RexClient
    {
        private static string _rexUrl;
        //private static TokenAuthManager _tokenManager;

        private static string _clientId;
        private static string _clientSecret;
        private static string _authority;

        static RexClient()
        {
            try
            {
                _rexUrl = ConfigurationManager.AppSettings["RexUrl"];
            
                _clientId = ConfigurationManager.AppSettings["AADClientID"];
                _clientSecret = ConfigurationManager.AppSettings["AADClientSecret"];
                _authority = ConfigurationManager.AppSettings["AADAuthority"];
            }
            catch { }
        }

        #region Apply/Execute Actions
        public static async Task<T> Apply<T>(string repositoryRuleAppName, string entityName, T initialEntityState) where T : new()
        {
            var ruleApp = new Ruleapp()
            {
                RepositoryRuleAppRevisionSpec = new Repositoryruleapprevisionspec()
                {
                    RuleApplicationName = repositoryRuleAppName
                }
            };
            return await Apply<T>(ruleApp, entityName, initialEntityState);
        }
        public static async Task<T> Apply<T>(Ruleapp ruleApp, string entityName, T initialEntityState) where T : new()
        {
            RuleRequest request = new ApplyRulesRequest()
            {
                RuleApp = ruleApp,
                EntityName = entityName
            };
            return await CallService(request, initialEntityState);
        }

        public static async Task<T> Execute<T>(string repositoryRuleAppName, string entityName, string ruleSetName, T initialEntityState) where T : new()
        {
            var ruleApp = new Ruleapp()
            {
                RepositoryRuleAppRevisionSpec = new Repositoryruleapprevisionspec()
                {
                    RuleApplicationName = repositoryRuleAppName
                }
            };
            return await Execute<T>(ruleApp, entityName, ruleSetName, initialEntityState);
        }
        public static async Task<T> Execute<T>(Ruleapp ruleApp, string entityName, string ruleSetName, T initialEntityState) where T : new()
        {
            RuleRequest request = new ExecuteRuleSetRequest()
            {
                RuleApp = ruleApp,
                EntityName = entityName,
                RuleSetName = ruleSetName
            };
            return await CallService(request, initialEntityState);
        }
        #endregion

        #region Helpers
        private static async Task<T> CallService<T>(RuleRequest request, T initialEntityState) where T : new()
        {
            try
            {
                request.EntityState = JsonConvert.SerializeObject(initialEntityState);
                string stringContent = JsonConvert.SerializeObject(request);

                request.RuleEngineServiceOutputTypes = new Ruleengineserviceoutputtypes
                {
                    ActiveNotifications = true,
                    ActiveValidations = true,
                    RuleExecutionLog = false,
                    EntityState = true,
                    Overrides = true
                };

                if (string.IsNullOrEmpty(_rexUrl))
                    throw new Exception("RexUrl Configuration Setting has not been defined.");

                string responseString = await Post(_rexUrl + "/HttpService.svc/" + request.Route, stringContent);

                if(string.IsNullOrEmpty(responseString))
                {
                    return new T();
                }

                RuleExecutionResponse responseObject = JsonConvert.DeserializeObject<RuleExecutionResponse>(responseString);
                string entityStateString = responseObject.EntityState;
                T finalEntityState = JsonConvert.DeserializeObject<T>(entityStateString);

                return finalEntityState;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new T();
            }
        }
        private static async Task<string> Post(string targetUrl, string stringContent, bool logRaw = false)
        {
            StringContent content = new StringContent(stringContent, Encoding.UTF8, "application/json");
            string responseString = "";

            if (logRaw)
            {
                Console.WriteLine("");
                Console.WriteLine($"Posting to {targetUrl} with content");
                Console.WriteLine(FormatJson(stringContent));
            }

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(targetUrl, content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    if (string.IsNullOrEmpty(_clientId))
                        throw new Exception("AADClientID Configuration Setting has not been defined.");
                    if (string.IsNullOrEmpty(_clientSecret))
                        throw new Exception("AADClientSecret Configuration Setting has not been defined.");
                    if (string.IsNullOrEmpty(_authority))
                        throw new Exception("AADAuthority Configuration Setting has not been defined.");

                    var clientCredential = new ClientCredential(_clientId, _clientSecret);
                    var context = new AuthenticationContext(_authority, false);
                    var authenticationResult = await context.AcquireTokenAsync(_clientId, clientCredential);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);

                    response = await client.PostAsync(targetUrl, content);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Unauthorized");
                    responseString = await response.Content.ReadAsStringAsync();
                    responseString = "";
                }
                else
                {
                    responseString = await response.Content.ReadAsStringAsync();
                }
            }

            if (logRaw)
            {
                Console.WriteLine("");
                Console.WriteLine("Received response:");
                Console.WriteLine(FormatJson(responseString));
            }

            return responseString;
        }
        private static string FormatJson(string json)
        {
            try
            {
                return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }
        #endregion
    }
}
