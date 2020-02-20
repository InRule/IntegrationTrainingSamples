using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace IntegrationTrainingSamples.Helpers
{
    public class ServiceHelpers
    {
        internal static WebServiceHost StartServiceEndpoint()
        {
            try
            {
                var host = new WebServiceHost(typeof(Program), new Uri("http://localhost:8000"));
                ServiceEndpoint ep = host.AddServiceEndpoint(typeof(IService), new WebHttpBinding() { CrossDomainScriptAccessEnabled = true }, "");
                host.Open();
                Console.WriteLine("Service is up and running at " + ep.Address);
                return host;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot start service host");
                return null;
            }
        }
    }
}
