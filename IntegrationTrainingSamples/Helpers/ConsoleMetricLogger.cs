using System;
using System.Threading.Tasks;
using InRule.Runtime;

namespace IntegrationTrainingSamples.Helpers
{
    public class ConsoleMetricLogger : IMetricLogger
    {
        public ConsoleMetricLogger()
        {
        }

        public async Task LogMetricsAsync(string serviceName, string ruleApplicationName, Guid sessionId, Metric[] metrics)
        {
            LogMetricsToConsole(serviceName, ruleApplicationName, sessionId, metrics);
        }

        public void LogMetrics(string serviceName, string ruleApplicationName, Guid sessionId, Metric[] metrics)
        {
            LogMetricsToConsole(serviceName, ruleApplicationName, sessionId, metrics);
        }

        private void LogMetricsToConsole(string serviceName, string ruleApplicationName, Guid sessionId, Metric[] metrics)
        {
            Console.WriteLine($"Service {serviceName} executed Rule App {ruleApplicationName} in session {sessionId} with metrics:");
            foreach (var metric in metrics)
            {
                Console.WriteLine($"  {metric.EntityName} ({metric.EntityId}):");
                foreach (var property in metric.Schema.Properties)
                {
                    Console.WriteLine($"    {(property.IsRule ? "Rule " : "")}{property.Name}:{metric[property]} ({property.DataType.ToString()})");
                }
            }
        }
    }
}