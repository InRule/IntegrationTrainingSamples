using InRule.Repository;
using IntegrationTrainingSamples.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTrainingSamples.RuleEngines
{
    public class DecisionServiceClient
    {
        private static TokenAuthManager _tokenManager;
        private static string _dsPublishUrl = null;

        static DecisionServiceClient()
        {
            try
            {
                var tokenUrl = ConfigurationManager.AppSettings["DecisionServicesTokenURL"];
                var clientId = ConfigurationManager.AppSettings["DecisionServicesClientID"];
                var clientSecret = ConfigurationManager.AppSettings["DecisionServicesClientSecret"];

                _tokenManager = new TokenAuthManager(tokenUrl, clientId, clientSecret, "master_service");

                _dsPublishUrl = ConfigurationManager.AppSettings["DecisionServicesPublishURL"];
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error starting up Decision Services Client config:" + ex.Message);
            }
        }

        public static async Task<bool> PublishDecision(string decisionName, RuleApplicationDef ruleApplication, bool isRetry = false)
        {
            Console.WriteLine($"Publishing Decision '{decisionName}'...");

            bool result = false;
            using (HttpClient client = new HttpClient())
            {
                string decisionJson = JsonConvert.SerializeObject(new
                {
                    name = decisionName,
                    ruleApplication = ruleApplication.GetXml()
                });

                var request = new HttpRequestMessage(HttpMethod.Post, _dsPublishUrl);
                request.Headers.Add("Authorization", $"Bearer {await _tokenManager.GetBearerToken()}");
                request.Content = new StringContent(decisionJson, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string reason = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error {(int)response.StatusCode} ({response.StatusCode.ToString()}): {reason}");

                    if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized && isRetry == false)
                    {
                        Console.WriteLine($"Received an Unauthorized response, retrieving new token and retrying request...");
                        _tokenManager.ResetBearerToken();
                        result = await PublishDecision(decisionName, ruleApplication, true);
                    }

                    result = false;
                }
                else
                {
                    result = true;
                }
            }

            return result;
        }

        public static async Task<string> ExecuteDecisionService(string decisionName, string inputJson, bool isRetry = false)
		{
			Console.WriteLine($"Executing '{decisionName}' Decision Service with inputs: {inputJson}");

			string result = "";
            using (HttpClient client = new HttpClient())
            {
	            var request = new HttpRequestMessage(HttpMethod.Post, $"{_dsPublishUrl}/{decisionName}");
	            request.Headers.Add("Authorization", $"Bearer {await _tokenManager.GetBearerToken()}");
	            request.Content = new StringContent(inputJson, Encoding.UTF8, "application/json");

	            var response = await client.SendAsync(request);
	            if (!response.IsSuccessStatusCode)
	            {
		            string reason = await response.Content.ReadAsStringAsync();
		            result = $"Error {(int)response.StatusCode} ({response.StatusCode.ToString()}): {reason}";
		            Console.WriteLine($"ERROR: {result}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && isRetry == false)
                    {
                        Console.WriteLine($"Received an Unauthorized response, retrieving new token and retrying request...");
                        _tokenManager.ResetBearerToken();
                        result = await ExecuteDecisionService(decisionName, inputJson, true);
                    }
                }
	            else
	            {
		            string outputJson = await response.Content.ReadAsStringAsync();
		            Console.WriteLine($"{decisionName} output: {outputJson}");
		            result = outputJson;
	            }
            }

			return result;
		}
    }
}
