using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IntegrationTrainingSamples.Model
{
    public class MultiplicationProblem
    {
        public double FactorA;
        public double FactorB;
        public double Result;
        public override string ToString()
        {
            return $"{FactorA} + {FactorB} = {Result}";
        }

        public static MultiplicationProblem Parse(string json)
        {
            dynamic responseObject = JObject.Parse(json);
            string entityStateString = responseObject.EntityState;
            MultiplicationProblem entityState = JsonConvert.DeserializeObject<MultiplicationProblem>(entityStateString);

            return entityState;
        }
    }
}
