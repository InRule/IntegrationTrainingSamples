// This is an example of an Azure Function that returns information about Rule Applications or Users in an irCatalog.
// It gets called by passing a query parameter with Users to retrieve the list of Users, or nothing to retrieve Rule Applications, as well as a Payload with Catalog info:
//      Uri : Catalog URI that should end with /service.svc/core
//      Username : Username to use to authenticate with the Catalog
//      Password : Password to use to authenticate with the Catalog


//These are not needed in Function (only here because we're not in a WebAPI project)
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Linq;
using System.Collections.Generic;

//These ARE all required in Function (including commented out items)
//#r "Newtonsoft.Json"
//using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using InRule.Repository;
using InRule.Repository.Client;

//Namespace wrapper not needed in Function
namespace IntegrationTrainingSamples.Functions
{
    //Class wrapper not needed in Function
    public class RetrieveCatalogInfoFunction
    {
        public static async Task<HttpResponseMessage> Run(HttpRequest req, ILogger log)
        {
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var creds = JsonConvert.DeserializeObject<CatalogCredentials>(content);

            try
            {
                var catCon = new RuleCatalogConnection(new Uri(creds.Uri), TimeSpan.FromSeconds(60), creds.Username, creds.Password, RuleCatalogAuthenticationType.BuiltIn);
                if (!string.IsNullOrEmpty(req.Query["users"])) //If requested, send user info
                {
                    var userDetails = catCon.GetUsers(false).Select(user => new UserDetails()
                    {
                        User = user,
                        Roles = catCon.GetRolesForUser(user)
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(userDetails), Encoding.UTF8, "application/json")
                    };
                }
                else //By default, send Rule App info
                {
                    var ruleAppData = catCon.GetRuleAppSummary(true).Select(ruleApp => new RuleAppDetails()
                    {
                        RuleAppInfo = ruleApp,
                        History = catCon.GetCheckinHistoryForDef(ruleApp.AppGuid).ToDictionary(k => k.Key, v => v.Value)
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(ruleAppData), Encoding.UTF8, "application/json")
                    };
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Error connecting to Catalog: " + ex.Message, Encoding.UTF8, "application/json")
                };
            }
        }
        class RuleAppDetails
        {
            public InRule.Repository.Service.Data.RuleAppInfo RuleAppInfo;
            public IDictionary<int, InRule.Repository.Service.Data.CheckinInfo> History;
        }
        class UserDetails
        {
            public InRule.Security.RuleUser User;
            public InRule.Security.RuleUserRole[] Roles;
        }
        class CatalogCredentials
        {
            public string Uri;
            public string Username;
            public string Password;
        }


        //This is a dummy wrapper not needed in Function (only here because we're not in a WebAPI project)
        public class HttpRequest
        {
            public string Body;
            public Dictionary<string, string> Query;
        }
    }
}