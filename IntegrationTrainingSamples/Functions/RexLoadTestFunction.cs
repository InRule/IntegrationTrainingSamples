// This is an example of an Azure Function that allows load-testing of an irServer Rule Execution Service (RES) using JSON requests.
// It gets called with the exact same body as a standard RES execution, with the addition of the following additional fields:
//      ExecutionServiceUrl : The FULL URL that the execution requests will be sent to; should end with /ApplyRules, /ExecuteRuleset, /ExecuteDecision, etc
//      AuthHeaderValue : For SaaS instances, this is the value to be used in the Authorization header
//      TotalExecutionCount : The total number of executions to run through irServer
//      ThreadCount : The max number of requests that will be performed in parallel at a time; to execute sequentially, set this to 1


//These are not needed in Function (only here because we're not in a WebAPI project)
using System.Net.Http;
using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.Extensions.Logging;

//These ARE all required in Function (including commented out items)
//#r "Newtonsoft.Json"
//using Microsoft.AspNetCore.Mvc;
//private static HttpClient httpClient = new HttpClient();
using System.Net;
using System.Text;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;

//Namespace wrapper not needed in Function
namespace IntegrationTrainingSamples.Functions
{
    //Class wrapper not needed in Function
    public class RexLoadTestFunction
    {
        public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
        {
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var config = JsonConvert.DeserializeObject<LoadTestConfiguration>(content);
            if (config.ThreadCount <= 0) //Cannot have 0 threads, or it will infinitely loop - default to 1
                config.ThreadCount = 1;

            List<double> _executionTimes = new List<double>();
            Dictionary<string, int> _resultStatusCodes = new Dictionary<string, int>();

            // Individual test logic
            Func<Task<string>> anonFunction = async () =>
            {
                var requestStartTime = DateTime.Now;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    if (!string.IsNullOrEmpty(config.AuthHeaderValue))
                        client.DefaultRequestHeaders.Add("Authorization", config.AuthHeaderValue);

                    StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");
                    var result = await client.PostAsync(config.ExecutionServiceUrl, stringContent);

                //log.LogInformation(result.Content.ReadAsStringAsync().Result);
                var statusCode = result.StatusCode.ToString();
                    if (_resultStatusCodes.ContainsKey(statusCode))
                        _resultStatusCodes[statusCode]++;
                    else
                        _resultStatusCodes[statusCode] = 1;
                }
                var elapsedTime = (DateTime.Now - requestStartTime).TotalMilliseconds;
                _executionTimes.Add(elapsedTime);
                return "done";
            };

            // Warm Up
            log.LogInformation($"Warming Up...");
            await anonFunction();
            await anonFunction();
            await anonFunction();
            // Clear Results
            _executionTimes = new List<double>();
            _resultStatusCodes = new Dictionary<string, int>();

            //Prepare test
            log.LogInformation($"Starting test of {config.TotalExecutionCount} executions using {config.ThreadCount} threads");
            var tasks = new List<Task<string>>();
            var totalRequestsRemaining = config.TotalExecutionCount;
            var startTime = DateTime.Now;

            // Load Threads
            while (tasks.Count < config.ThreadCount && totalRequestsRemaining > 0)
            {
                totalRequestsRemaining--;
                tasks.Add(anonFunction());
            }
            // Continue running tests until we've loaded the total number of executions requested
            while (totalRequestsRemaining > 0)
            {
                await Task.WhenAny(tasks);
                tasks.RemoveAll(t => t.Status == TaskStatus.RanToCompletion);
                while (tasks.Count < config.ThreadCount)
                {
                    totalRequestsRemaining--;
                    tasks.Add(anonFunction());
                }
            }
            // Wait until all pending executions have completed
            await Task.WhenAll(tasks);

            // Return test summary info
            var totalExecutionTime = (DateTime.Now - startTime).TotalMilliseconds;
            var overallSummary = $"Completed {config.TotalExecutionCount} requests with {config.ThreadCount} threads in {Math.Round(totalExecutionTime, 0)}ms\n";
            var responseCodeSummary = "Response Codes Received: " + string.Join(", ", _resultStatusCodes.Select(r => r.Key + ":" + r.Value)) + "\n";
            var statisticSummary = $"Individual Response Time (ms) Avg:{Math.Round(_executionTimes.Average(), 0)}, Max:{Math.Round(_executionTimes.Max(), 0)}, Min:{Math.Round(_executionTimes.Min(), 0)}";
            return (ActionResult)new OkObjectResult(overallSummary + responseCodeSummary + statisticSummary);
        }
        class LoadTestConfiguration
        {
            public string ExecutionServiceUrl; // The FULL URL that the execution requests will be sent to
            public string AuthHeaderValue; // For SaaS instances, this is the value to be used in the Authorization header
            public int TotalExecutionCount; // The total number of executions to run through irServer
            public int ThreadCount;// The max number of requests that will be performed in parallel at a time; to execute sequentially, set this to 1
        }


        //These are dummy wrappers not needed in Function (only here because we're not in a WebAPI project)
        public class HttpRequest
        {
            public string Body;
        }
        public class IActionResult
        {
        }
        public class ActionResult : IActionResult
        {
        }
        public class OkObjectResult : ActionResult
        {
            public OkObjectResult(string message)
            {
            }
        }
    }
}