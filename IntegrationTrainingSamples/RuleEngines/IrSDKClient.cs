using InRule.Runtime;
using IntegrationTrainingSamples.Model;
using Newtonsoft.Json;
using System;

namespace IntegrationTrainingSamples.RuleEngines
{
    public class IrSDKClient
    {
        public static T InvokeEngine<T>(IrCatalogConnectionSettings catalog, string ruleAppName, string entityName, T initialEntityState,
                                            string ruleSetName = null, string label = null, EngineLogOptions engineLogOptions = EngineLogOptions.None, bool log = true)
        {
            try
            {
                //Step 1 - Retrieve Rule App Reference
                RuleApplicationReference ruleAppRef = new CatalogRuleApplicationReference(catalog.Url, ruleAppName, catalog.Username, catalog.Password, label);

                //Step 2 - Create RuleSession
                using (var session = new RuleSession(ruleApplicationReference: ruleAppRef))
                {
                    //Step 3 - Define Execution Settings (optional)
                    session.Settings.LogOptions = engineLogOptions;// EngineLogOptions.SummaryStatistics | EngineLogOptions.DetailStatistics | EngineLogOptions.StateChanges;

                    //Override endpoint configuration
                    //var endpoints = session.RuleApplication.GetRuleApplicationDef().EndPoints.ToList();
                    //if (endpoints.Any(e => e.EndPointType == InRule.Repository.EndPoints.EndPointType.RestService && e.Name == "ExchangeRateService"))
                    //    session.Overrides.OverrideRestServiceRootUrl("ExchangeRateService", "https://api.exchangeratesapi.io");

                    //Step 4 - Pass Data
                    Entity entity;
                    if(typeof(T) == typeof(string)) //supports JSON data passed in as a string
                        entity = session.CreateEntity(entityName, (string)(object)initialEntityState, EntityStateType.Json);
                    else
                        entity = session.CreateEntity(entityName, initialEntityState);

                    //Step 5 - Execute Rules
                    if (string.IsNullOrEmpty(ruleSetName))
                        session.ApplyRules();
                    else
                        entity.ExecuteRuleSet(ruleSetName);

                    //Step 6 - Process Output
                    if (typeof(T) == typeof(string))
                        initialEntityState = (T)(object)entity.GetJson();

                    if (log)
                    {
                        foreach (var notification in session.GetNotifications())
                            Console.WriteLine($"Notification {notification.Type}: {notification.Message}");

                        foreach (var validation in session.GetValidations())
                            Console.WriteLine($"Validation error on {validation.Target}: {validation.ActiveReasons}");

                        Console.WriteLine($"Rule executed in {session.Statistics.RuleExecutionTime.Last.TotalMilliseconds}ms");

                        //Console.WriteLine(initialEntityState.ToString());
                    }

                    var executionLog = session.LastRuleExecutionLog;
                    var stateChanges = executionLog.StateValueChanges;
                    var statistics = executionLog.Statistics;
                    //var executionTrace = session.LastRuleExecutionLog.GetExecutionTrace();
                }
                return initialEntityState;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return default(T);
            }
        }

        public static O ExecuteDecision<I, O>(IrCatalogConnectionSettings catalog, string ruleAppName, string decisionName, I input, string label = null, bool log = true) where O : new()
        {
            O output = new O();
            try
            {
                //Step 1 - Retrieve Rule App Reference
                RuleApplicationReference ruleAppRef = new CatalogRuleApplicationReference(catalog.Url, ruleAppName, catalog.Username, catalog.Password, label);

                //Step 2 - Create RuleSession
                using (var session = new RuleSession(ruleApplicationReference: ruleAppRef))
                {
                    //Step 3 - Define Execution Settings (optional)
                    session.Settings.LogOptions = EngineLogOptions.SummaryStatistics;

                    //Step 4 - Create Decision
                    var decision = session.CreateDecision(decisionName);

                    //Step 5 - Execute Decision
                    string inputJson = JsonConvert.SerializeObject(input);
                    DecisionResult result = decision.Execute(inputJson, EntityStateType.Json);

                    //Step 6 - Process Output
                    var outputJson = result.ToJson();
                    output = JsonConvert.DeserializeObject<O>(outputJson);

                    if (log)
                    {
                        foreach (var notification in session.GetNotifications())
                            Console.WriteLine($"Notification {notification.Type}: {notification.Message}");

                        foreach (var validation in session.GetValidations())
                            Console.WriteLine($"Validation error on {validation.Target}: {validation.ActiveReasons}");

                        Console.WriteLine($"Rule executed in {session.Statistics.RuleExecutionTime.Last.TotalMilliseconds}ms");

                        //Console.WriteLine(initialEntityState.ToString());
                    }

                    var executionLog = session.LastRuleExecutionLog;
                    var stateChanges = executionLog.StateValueChanges;
                    var statistics = executionLog.Statistics;
                    //var executionTrace = session.LastRuleExecutionLog.GetExecutionTrace();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return output;
        }
    }
}
