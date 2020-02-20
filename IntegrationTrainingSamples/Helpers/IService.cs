using System.ServiceModel;
using System.ServiceModel.Web;

namespace IntegrationTrainingSamples
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        double Multiply(double factorA, double factorB);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        double MultiplyWithRounding(double factorA, double factorB);
    }
}
