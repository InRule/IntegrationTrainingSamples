using System;
using InRule.Runtime;
using System.ServiceModel.Web;
using InRule.Repository;
using InRule.Repository.RuleElements;
using InRule.Repository.Client;
using System.Linq;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using IntegrationTrainingSamples.Model;
using IntegrationTrainingSamples.Helpers;
using IntegrationTrainingSamples.RuleEngines;
using InRule.Repository.Regression;
using InRule.Runtime.Testing.Regression;
using InRule.Runtime.Testing.Session;
using InRule.Runtime.Testing.Regression.Runtime;
using System.Text;
using InRule.Authoring.BusinessLanguage;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Configuration;
using Newtonsoft.Json;
using InRule.Authoring.Extensions;
using InRule.Authoring.BusinessLanguage.Tokens;
using InRule.Repository.Decisions;
using InRule.Repository.EndPoints;
using InRule.Repository.Vocabulary;

namespace IntegrationTrainingSamples
{
    class Program : IService
    {
        #region Variables and Startup
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IrCatalogConnectionSettings _catalog = null;
        private WebServiceHost _host;

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            //_logger.Debug("Testing log4net");

            var program = new Program();
            program.Start();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Commands:");
                Console.WriteLine("1: Demonstrate Cold Start Penalty");
                Console.WriteLine("   1.1: Limit Rule App cache size");
                Console.WriteLine("   1.2: Reset Rule App cache to Default");
                Console.WriteLine("   1.3: Pre-Compile Rule App");
                Console.WriteLine("2: List Rule Apps from Catalog");
                Console.WriteLine("   2.1: List Rule Sets");
                Console.WriteLine("   2.2: List Business Language Logic");
                Console.WriteLine("3: Check Out Rule App");
                Console.WriteLine("   3.1: Undo Checkout of Rule App");
                Console.WriteLine("   3.2: Search Catalog for Text 'Set'");
                Console.WriteLine("4: Build and execute in-memory Rule App");
                Console.WriteLine("5: Execute Regression Test Suite");
                Console.WriteLine("6: Retrieve IrJS from Distribution Service");
                Console.WriteLine("7: Run with Metrics Logger attached");
                Console.WriteLine("8: Execute a Decision");
                Console.WriteLine("   8.1: Build and Publish a Decision");
                Console.WriteLine("9: Apply Rules via REX");

                var requestType = Console.ReadLine();
                bool exit = false;

                switch (requestType)
                {
                    case "1":
                        program.ColdStartDemo();
                        break;
                    case "1.1":
                        program.LimitCacheSize();
                        break;
                    case "1.2":
                        program.ResetCache();
                        break;
                    case "1.3":
                        program.PreCompile();
                        break;
                    case "2":
                        program.ListRuleApps();
                        break;
                    case "2.1":
                        program.ListRuleSetDetails("MortgageCalculator");
                        break;
                    case "2.2":
                        program.LogBusinessLanguageText(@"..\..\..\RuleApps\MortgageCalculator with REST Lookup.ruleappx");
                        break;
                    case "3":
                        program.CheckOutRuleApp();
                        break;
                    case "3.1":
                        program.UndoCheckout();
                        break;
                    case "3.2":
                        foreach (var result in program.SearchCatalogForDescription(SearchField.Name, "Set"))
                            Console.WriteLine(result);
                        break;
                    case "4":
                        program.BuildAndRunInMemoryRuleApp();
                        break;
                    case "5":
                        program.RunTestSuite(@"..\..\..\RuleApps\MultiplicationApp.ruleappx",
                                             @"..\..\..\RuleApps\MultiplicationApp Test Suite.testsuite");
                        break;
                    case "6":
                        program.RetrieveIrJSFromDistributionService();
                        break;
                    case "7":
                        program.ExecuteWithMetricsLogged();
                        break;
                    case "8":
                        var er = program.ExecuteDecision("MortgageSummary", "{ \"Principal\": 100000, \"APR\": 0.9, \"TermInYears\": 30 }").Result;
                        break;
                    case "8.1":
                        var br = program.BuildAndPublishDecision().Result;
                        break;
                    case "9":
                        var ar = RexClient.Apply("MultiplicationApp", "MultiplicationProblem", new MultiplicationProblem() { FactorA = 4, FactorB = 32 }).Result;
                        Console.WriteLine($"{ar.FactorA} * {ar.FactorB} = {ar.Result}");
                        break;
                    default:
                        exit = true;
                        break;
                }
                if (exit)
                    break;
            }

            program.Stop();
        }
        public void Start()
        {
            try
            {
                _catalog = new IrCatalogConnectionSettings()
                {
                    Url = ConfigurationManager.AppSettings["CatalogUrl"],
                    Username = ConfigurationManager.AppSettings["CatalogUsername"],
                    Password = ConfigurationManager.AppSettings["CatalogPassword"]
                };
            }
            catch (Exception ex)
            {
                _logger.Debug("Error loading config from configuration file: ", ex);
            }
            _host = ServiceHelpers.StartServiceEndpoint();
        }
        public void Stop()
        {
            if (_host != null)
                _host.Close();
        }

        private enum LibraryColumns : int
        {
            ID = 0,
            Title = 1,
            URL = 2,
            Description = 3,
            Type = 4,
            URLID = 5
        }
        #region test classes
        [Serializable()]
        public partial class Transaction
        {
            private List<Insured> _insureds = new List<Insured>();
            private System.DateTime _transEffDate;
            private System.DateTime _transExpDate;
            public virtual List<Insured> Insureds
            {
                get
                {
                    return this._insureds;
                }
            }
            public virtual System.DateTime TransEffDate
            {
                get
                {
                    return this._transEffDate;
                }
                set
                {
                    this._transEffDate = value;
                }
            }
            public virtual System.DateTime TransExpDate
            {
                get
                {
                    return this._transExpDate;
                }
                set
                {
                    this._transExpDate = value;
                }
            }
        }
        [Serializable()]
        public partial class Insured
        {
            private int _insuredId;
            private string _insuredTypeId;
            private List<Specialty> _specialties = new List<Specialty>();
            public virtual int InsuredId
            {
                get
                {
                    return this._insuredId;
                }
                set
                {
                    this._insuredId = value;
                }
            }
            public virtual string InsuredTypeId
            {
                get
                {
                    return this._insuredTypeId;
                }
                set
                {
                    this._insuredTypeId = value;
                }
            }
            public virtual List<Specialty> Specialties
            {
                get
                {
                    return this._specialties;
                }
            }
        }
        [Serializable()]
        public partial class Specialty
        {
            private string _specialtyCode;
            private List<Coverage> _coverages = new List<Coverage>();
            public virtual string SpecialtyCode
            {
                get
                {
                    return this._specialtyCode;
                }
                set
                {
                    this._specialtyCode = value;
                }
            }
            public virtual List<Coverage> Coverages
            {
                get
                {
                    return this._coverages;
                }
            }
        }
        [Serializable()]
        public partial class Coverage
        {
            private int _coverageId;
            private string _coverageTypeId;
            private bool _isActive;
            private string _coverageCode;
            private string _coverageCodeId;
            private int _coverageAmt;
            private int _incidentLimit;
            private int _incidentLimitAmt;
            private int _aggregateLimit;
            private int _aggregateLimitAmt;
            private string _commissionCode;
            private decimal _commissionPct;
            private string _premiumCode;
            private decimal _premiumAmt;
            private string _uWCode;
            private decimal _uWFee;
            private string _stateID;
            private decimal _stateTax;
            private string _ratingBasisId;
            private int _ratingBasisAmount;
            public virtual int CoverageId
            {
                get
                {
                    return this._coverageId;
                }
                set
                {
                    this._coverageId = value;
                }
            }
            public virtual string CoverageTypeId
            {
                get
                {
                    return this._coverageTypeId;
                }
                set
                {
                    this._coverageTypeId = value;
                }
            }
            public virtual bool IsActive
            {
                get
                {
                    return this._isActive;
                }
                set
                {
                    this._isActive = value;
                }
            }
            public virtual string CoverageCode
            {
                get
                {
                    return this._coverageCode;
                }
                set
                {
                    this._coverageCode = value;
                }
            }
            public virtual string CoverageCodeId
            {
                get
                {
                    return this._coverageCodeId;
                }
                set
                {
                    this._coverageCodeId = value;
                }
            }
            public virtual int CoverageAmt
            {
                get
                {
                    return this._coverageAmt;
                }
                set
                {
                    this._coverageAmt = value;
                }
            }
            public virtual int IncidentLimit
            {
                get
                {
                    return this._incidentLimit;
                }
                set
                {
                    this._incidentLimit = value;
                }
            }
            public virtual int IncidentLimitAmt
            {
                get
                {
                    return this._incidentLimitAmt;
                }
                set
                {
                    this._incidentLimitAmt = value;
                }
            }
            public virtual int AggregateLimit
            {
                get
                {
                    return this._aggregateLimit;
                }
                set
                {
                    this._aggregateLimit = value;
                }
            }
            public virtual int AggregateLimitAmt
            {
                get
                {
                    return this._aggregateLimitAmt;
                }
                set
                {
                    this._aggregateLimitAmt = value;
                }
            }
            public virtual string CommissionCode
            {
                get
                {
                    return this._commissionCode;
                }
                set
                {
                    this._commissionCode = value;
                }
            }
            public virtual decimal CommissionPct
            {
                get
                {
                    return this._commissionPct;
                }
                set
                {
                    this._commissionPct = value;
                }
            }
            public virtual string PremiumCode
            {
                get
                {
                    return this._premiumCode;
                }
                set
                {
                    this._premiumCode = value;
                }
            }
            public virtual decimal PremiumAmt
            {
                get
                {
                    return this._premiumAmt;
                }
                set
                {
                    this._premiumAmt = value;
                }
            }
            public virtual string UWCode
            {
                get
                {
                    return this._uWCode;
                }
                set
                {
                    this._uWCode = value;
                }
            }
            public virtual decimal UWFee
            {
                get
                {
                    return this._uWFee;
                }
                set
                {
                    this._uWFee = value;
                }
            }
            public virtual string StateID
            {
                get
                {
                    return this._stateID;
                }
                set
                {
                    this._stateID = value;
                }
            }
            public virtual decimal StateTax
            {
                get
                {
                    return this._stateTax;
                }
                set
                {
                    this._stateTax = value;
                }
            }
            public virtual string RatingBasisId
            {
                get
                {
                    return this._ratingBasisId;
                }
                set
                {
                    this._ratingBasisId = value;
                }
            }
            public virtual int RatingBasisAmount
            {
                get
                {
                    return this._ratingBasisAmount;
                }
                set
                {
                    this._ratingBasisAmount = value;
                }
            }
        }
        #endregion
        #endregion

        #region Cold Start and Caching Demos
        private void ColdStartDemo()
        {
            var time1 = DateTime.UtcNow;
            Console.WriteLine($"Starting first rule evaluation with cache size of {RuleSession.RuleApplicationCache.Count}...");
            IrSDKApplyMultiplication(10, 10, false);
            var time2 = DateTime.UtcNow;
            Console.WriteLine($"Run 1 completed in {(time2 - time1).TotalMilliseconds}ms (cold start) with cache size of {RuleSession.RuleApplicationCache.Count}.  Starting run 2...");
            IrSDKApplyMultiplication(10, 10, false);
            var time3 = DateTime.UtcNow;
            Console.WriteLine($"Run 2 completed in {(time3 - time2).TotalMilliseconds}ms (partial cache) with cache size of {RuleSession.RuleApplicationCache.Count}.  Starting run 3...");
            IrSDKApplyMultiplication(10, 10, false);
            var time4 = DateTime.UtcNow;
            Console.WriteLine($"Run 3 completed in {(time4 - time3).TotalMilliseconds}ms (fully cached) with cache size of {RuleSession.RuleApplicationCache.Count}.  Starting run 4 after changing log settings...");
            IrSDKApplyMultiplication(10, 10, false, true);
            var time5 = DateTime.UtcNow;
            Console.WriteLine($"Run 4 completed in {(time5 - time4).TotalMilliseconds}ms (partial recompile) with cache size of {RuleSession.RuleApplicationCache.Count}.  Starting run 5 with new log settings...");
            IrSDKApplyMultiplication(10, 10, false, true);
            var time6 = DateTime.UtcNow;
            Console.WriteLine($"Run 5 completed in {(time6 - time5).TotalMilliseconds}ms (standard) with cache size of {RuleSession.RuleApplicationCache.Count}.  Running different Rule App...");
            IrSDKApplyJson("MortgageCalculator", "Mortgage", "{'LoanInfo': { 'Principal' : 400000, 'APR' : 3.1, 'TermInYears' : 30 }}");
            var time7 = DateTime.UtcNow;
            Console.WriteLine($"Run 6 completed in {(time7 - time6).TotalMilliseconds}ms (cold start) with cache size of {RuleSession.RuleApplicationCache.Count}.  Running another different Rule App...");
            IrSDKApplyJson("Werewolf", "WerewolfEncounter", "{'ZipCode': '60613', 'MilesFromGraveyard': 5}");
            var time8 = DateTime.UtcNow;
            Console.WriteLine($"Run 7 completed in {(time8 - time7).TotalMilliseconds}ms (cold start) with cache size of {RuleSession.RuleApplicationCache.Count}.  Running original Rule App...");
            IrSDKApplyMultiplication(10, 10, false);
            var time9 = DateTime.UtcNow;
            Console.WriteLine($"Run 8 completed in {(time9 - time8).TotalMilliseconds}ms (cold start) with cache size of {RuleSession.RuleApplicationCache.Count}.  Running original Rule App one more time...");
            IrSDKApplyMultiplication(10, 10, false);
            var time10 = DateTime.UtcNow;
            Console.WriteLine($"Run 9 completed in {(time10 - time9).TotalMilliseconds}ms (standard) with cache size of {RuleSession.RuleApplicationCache.Count}.");

        }
        private void LimitCacheSize()
        {
            RuleSession.RuleApplicationCache.ConfigureCachePolicy(new DefaultRuleApplicationCachePolicy(2));
            Console.WriteLine($"Cache size has been limited to length of 2.");
        }
        private void ResetCache()
        {
            RuleSession.RuleApplicationCache.ConfigureCachePolicy(new DefaultRuleApplicationCachePolicy());
            Console.WriteLine($"Cache size has been limited to length of {DefaultRuleApplicationCachePolicy.DefaultCacheDepth}.");
        }
        private void PreCompile()
        {
            Console.WriteLine("Compiling MultiplicationApp...");
            var ruleAppRef = GetCatalogRuleApp("MultiplicationApp");
            ruleAppRef.Compile(CacheRetention.AlwaysRetain);
            Console.WriteLine("Compiling MortgageCalculator...");
            var ruleAppRef1 = GetCatalogRuleApp("MortgageCalculator");
            ruleAppRef1.Compile(CacheRetention.FromWeight(1000));
            Console.WriteLine("Compiling Werewolf...");
            var ruleAppRef2 = GetCatalogRuleApp("Werewolf");
            ruleAppRef2.Compile();
        }
        #endregion

        #region List Demos
        public void ListRuleApps()
        {
            Console.WriteLine("Rule Apps contained in the catalog located at " + _catalog.Url);

            var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn);
            foreach (var ruleApp in catCon.GetAllRuleApps())
            {
                var ruleAppDefInfo = ruleApp.Key;
                var ruleAppInfo = ruleApp.Value;
                Console.WriteLine($"Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision} {(ruleAppDefInfo.IsLatest ? ruleAppInfo.LastLabelName : "") }");
            }
        }

        public void ListRuleSetDetails(string ruleAppName)
        {
            Console.WriteLine("RuleSets contained in the " + ruleAppName + " Rule App:");
            
            var ruleApp = new CatalogRuleApplicationReference(_catalog.Url, ruleAppName, _catalog.Username, _catalog.Password, "LIVE");
            var ruleAppDef = ruleApp.GetRuleApplicationDef();

            //var entityRuleSetDictionary = ruleAppDef.Entities.ToList<EntityDef>().ToDictionary(k => k.Name, v => v.GetAllRuleSets().Select(rs => rs.Name).ToList());

            foreach (EntityDef entity in ruleAppDef.Entities)
            {
                Console.WriteLine($"");
                Console.WriteLine($"Entity: {entity.Name}");

                if(!string.IsNullOrEmpty(entity.Comments))
                    Console.WriteLine($"    Description: {entity.Comments}");

                foreach (FieldDef field in entity.Fields)
                {
                    Console.WriteLine($"    Property: {field.Name} ({field.DataType})");
                    if (!string.IsNullOrEmpty(entity.Comments))
                        Console.WriteLine($"        Description: {field.Comments}");
                }

                foreach (RuleSetDef ruleSet in entity.GetAllRuleSets())
                {
                    var xml = InRule.Common.Utilities.XmlSerializationUtility.ObjectToXML(ruleSet);
                    Console.WriteLine($"RuleSet: {ruleSet.Name} ({ruleSet.FireMode.ToString()})" + GetDescriptionSuffix(ruleSet.Comments));

                    if (!string.IsNullOrEmpty(ruleSet.Comments))
                        Console.WriteLine($"    Description: {ruleSet.Comments}");

                    foreach (RuleSetParameterDef parameter in ruleSet.Parameters)
                        Console.WriteLine($"    Parameter: {parameter.Name} ({parameter.DataType})");

                    foreach(RuleRepositoryDefBase rule in ruleSet.GetAllRuleElements())
                        Console.WriteLine("    Rule: " + rule.Name + GetDescriptionSuffix(rule.Comments));
                }

                if (entity.Vocabulary != null && entity.Vocabulary.Templates != null && entity.Vocabulary.Templates.Any())
                {
                    Console.WriteLine($"");
                    Console.WriteLine($"Vocabulary: {entity.Vocabulary.Name}");
                    foreach (TemplateDef vocabTemplate in entity.Vocabulary.Templates)
                    {
                        Console.WriteLine($"    Vocab Template: {vocabTemplate.Name} ({vocabTemplate.TemplateType})");

                        if (!string.IsNullOrEmpty(vocabTemplate.Comments))
                            Console.WriteLine($"        Description: {vocabTemplate.Comments}");
                    }
                }
            }

            if (ruleAppDef.DataElements != null && ruleAppDef.DataElements.Count > 0)
            {
                Console.WriteLine($"");
                foreach (DataElementDef dataElement in ruleAppDef.DataElements)
                {
                    Console.WriteLine($"Data Element: {dataElement.Name} ({dataElement.DataElementType})");

                    if (!string.IsNullOrEmpty(dataElement.Comments))
                        Console.WriteLine($"    Description: {dataElement.Comments}");
                }
            }
        }
        private string GetDescriptionSuffix(string description)
        {
            if (string.IsNullOrEmpty(description))
                return "";
            else
                return ", Description: " + description;

        }

        private TemplateEngine _templateEngine;
        private void LogBusinessLanguageText(string filePath)
        {
            var ruleApp = new FileSystemRuleApplicationReference(filePath);
            var ruleAppDef = ruleApp.GetRuleApplicationDef();

            _templateEngine = new TemplateEngine();
            _templateEngine.LoadRuleApplicationAndEnabledStandardTemplates(ruleAppDef);
            
            foreach (EntityDef entity in ruleAppDef.Entities)
            {
                Console.WriteLine($"");
                Console.WriteLine($"Entity: {entity.Name}");

                foreach (var ruleSet in entity.GetAllRuleSets())
                {
                    Console.WriteLine($"  RuleSet: {ruleSet.Name} ({ruleSet.FireMode.ToString()})");

                    foreach (RuleRepositoryDefBase rule in ruleSet.GetAllRuleElements())
                    {
                        Console.WriteLine("    Rule: " + rule.Name);
                        if (rule is LanguageRuleDef languageRuleDef)
                        {
                            Console.WriteLine("      Business Logic:");

                            //BL Engine
                            var text = GetBusinessLanguageText(languageRuleDef, TextOutputFormat.RawText);
                            Console.WriteLine(text);

                            //Custom Logic
                            StringBuilder sb = new StringBuilder();
                            BuildLanguageRuleText(languageRuleDef, sb, "      ");
                            Console.WriteLine(sb.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
                        }
                    }
                }
            }
        }
        private void BuildLanguageRuleText(RuleRepositoryDefBase rule, StringBuilder output, string linePrefix)
        {
            output.Append(linePrefix);

            if (rule is FireNotificationActionDef)
                output.Append("Fire Notification: " + ((FireNotificationActionDef)rule).NotificationMessageText);
            else if (rule is LanguageRuleDef)
                //output.Append(InRule.Authoring.Reporting.RuleAppReport.GetBusinessLanguageText(((LanguageRuleDef)rule), _templateEngine, InRule.Authoring.BusinessLanguage.Tokens.TextOutputFormat.RawText));
                output.Append(((LanguageRuleDef)rule).RuleElement.AuthoringContextName);
            else if (rule is InRule.Repository.Vocabulary.TemplateValueDef)
                output.Append($"Vocab {rule.AuthoringContextName} called with params {string.Join(", ", ((InRule.Repository.Vocabulary.TemplateValueDef)rule).PlaceholderValues.Select(pv => pv.Value).ToList())}");
            else
                output.Append(rule.AuthoringContextName);

            output.AppendLine();

            if (rule.HasChildCollectionChildren)
            {
                bool requiresElsePrefix = false;
                foreach (var childRuleCollection in rule.GetAllChildCollections())
                {
                    if (requiresElsePrefix)
                    {
                        linePrefix = linePrefix + "  ";
                        output.Append(linePrefix);
                        output.Append("Else ");
                        output.AppendLine();
                    }
                    foreach (var childRule in childRuleCollection)
                    {
                        BuildLanguageRuleText((RuleRepositoryDefBase)childRule, output, linePrefix + "  ");
                    }
                    if (rule.GetAllChildCollections()[0][0].AuthoringElementTypeName == "If Then")
                    {
                        requiresElsePrefix = true;
                    }
                }
            }
        }
        public string GetBusinessLanguageText(LanguageRuleDef languageRuleDef, TextOutputFormat outputType)
        {
            string text;

            try
            {
                _templateEngine.LoadVocabularies(languageRuleDef);
                _templateEngine.LoadTemplateAvailability(languageRuleDef);

                string[] contexts = TemplateEngine.ResolveAllContextsFromDef(languageRuleDef);
                var rootToken = _templateEngine.CreateRootRuleElementToken(contexts, "DEFAULT");
                var hintTable = languageRuleDef.Attributes[RuleRepositoryDefBase.ReservedInRuleTokenKey];
                RuleRepositoryDefBase copyOfRule = languageRuleDef.RuleElement.CopyWithSameGuids();
                copyOfRule.SetParent(languageRuleDef);
                rootToken.InferFromRuleExpression(
                    _templateEngine,
                    new InRule.Authoring.BusinessLanguage.Builders.TemplateInputDef(copyOfRule),
                    hintTable);
                
                text = rootToken.TokenValue.GetFormattedText(outputType);
            }
            finally
            {
                //order matters here
                _templateEngine.UnloadVocabularies(languageRuleDef);
                _templateEngine.ResetNetworkCache();
            }
            return text;
        }

        private void ListRuleDetails()
        {
            var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn);
            foreach (var ruleApp in catCon.GetAllRuleApps())
            {
                var ruleAppDefInfo = ruleApp.Key;
                var ruleAppInfo = ruleApp.Value;

                if (!ruleAppDefInfo.IsLatest)
                    continue;
                
                Console.WriteLine($"");
                Console.WriteLine($"Rule App: {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision}");

                var ruleAppRef = new CatalogRuleApplicationReference(_catalog.Url, ruleAppDefInfo.Name, _catalog.Username, _catalog.Password, ruleAppDefInfo.PublicRevision);
                var ruleAppDef = ruleAppRef.GetRuleApplicationDef();

                foreach (EntityDef entity in ruleAppDef.Entities)
                {
                    Console.WriteLine($"  Entity: {entity.Name}");

                    foreach (var ruleSet in entity.GetAllRuleSets())
                    {
                        Console.WriteLine($"    RuleSet: {ruleSet.Name} ({ruleSet.FireMode.ToString()})" + GetDescriptionSuffix(ruleSet.Comments));

                        foreach (RuleSetParameterDef parameter in ruleSet.Parameters)
                            Console.WriteLine($"      Parameter: {parameter.Name} ({parameter.DataType})");
                    }
                }
            }
        }
        #endregion

        #region Catalog Interaction Demos
        private void AddRuleAppToCatalog()
        {
            Console.WriteLine("Perparing to add new Rule Application to the Catalog...");
            var newRuleAppDef = RuleApplicationDef.Load(@"..\..\..\RuleApps\NewCheckinTest.ruleappx");
            var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn);
            
            //This will throw an exception if a Rule App already exists with the same GUID
            catCon.CreateRuleApplication(newRuleAppDef, "Created new catalog entry for " + newRuleAppDef.Name);
            //catCon.ApplyLabel(newRuleAppDef, "LIVE"); //Optional, based on needs
            
            Console.WriteLine("Addition complete!");
        }
        private void OverwriteRuleAppInCatalog()
        {
            Console.WriteLine("Perparing to Update Rule Application in the Catalog...");
            var updatedRuleAppDef = RuleApplicationDef.Load(@"..\..\..\RuleApps\NewCheckinTest.ruleappx");
            var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn);

            // OPTION 1 : If the same file is consistently used, you can simply check out and back in with the same file - the GUID is used for lookup
            catCon.CheckoutRuleApplicationAndSchema(updatedRuleAppDef, "Automated Update");
            catCon.Checkin(updatedRuleAppDef, "Automated Push");

            // OPTION 2 : You can overwrite the full Rule App in the catalog reguardless of what the source Rule App is
            //var existingRuleApp = catCon.GetRuleAppRef("NewRuleApplication");
            //catCon.OverwriteRuleApplication(existingRuleApp.Guid, updatedRuleAppDef, true, "Overwritten version");

            //catCon.ApplyLabel(newRuleAppDef, "LIVE"); //Optional, based on needs
            Console.WriteLine("Update complete!");
        }
        private void CheckOutRuleApp()
        {
            Console.WriteLine("Checking out MultiplicationApp...");
            var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn);
            var appRef = catCon.GetRuleAppRef("MultiplicationApp");
            var appDef = catCon.GetLatestRuleAppRevision(appRef.Guid);
            catCon.CheckoutRuleApplication(appDef, true, "Testing code-based catalog interactions");
            Console.WriteLine("Checked out MultiplicationApp.");

            catCon.Checkin(appDef, "Demo checkout/checkin");
            Console.WriteLine("Checked in MultiplicationApp.");
        }
        private void ModifyRuleAppWithCheckin()
        {
            Console.WriteLine("Checking out MultiplicationApp...");
            var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn);
            var appRef = catCon.GetRuleAppRef("MultiplicationApp");
            var appDef = catCon.GetLatestRuleAppRevision(appRef.Guid);
            catCon.CheckoutRuleApplication(appDef, true, "Updating data connections for new environment");

            Console.WriteLine("Modifying MultiplicationApp...");
            if (appDef.EndPoints.Any(e => e.EndPointType == EndPointType.DatabaseConnection && e.Name == "MyDbConnection"))
                ((DatabaseConnection)appDef.EndPoints["MyDbConnection"]).ConnectionString = "newConnectionString";

            if (appDef.EndPoints.Any(e => e.EndPointType == EndPointType.RestService && e.Name == "MyRestService"))
                ((RestServiceDef)appDef.EndPoints["MyRestService"]).RootUrl = "newRestRootUrl";

            var fileApp = new FileSystemRuleApplicationReference("").GetRuleApplicationDef();
            Console.WriteLine("Checking in MultiplicationApp...");
            catCon.Checkin(appDef, "Updated data connections for new environment");
            Console.WriteLine("Checked in MultiplicationApp.");
        }
        private void UndoCheckout()
        {
            Console.WriteLine("Undoing Checkout of MultiplicationApp...");
            var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn);
            var appRef = catCon.GetRuleAppRef("MultiplicationApp");
            var appDef = catCon.GetLatestRuleAppRevision(appRef.Guid);
            catCon.UndoRuleAppCheckout(appDef);
            Console.WriteLine("Undid Checkout of MultiplicationApp.");
        }

        public List<string> SearchCatalogForDescription(SearchField field, string searchQuery)
        {
            List<string> results = new List<string>();

            Console.WriteLine($"Searching for '{searchQuery}' in the {field} from catalog located at {_catalog.Url}");

            using (var catCon = new RuleCatalogConnection(new Uri(_catalog.Url), TimeSpan.FromSeconds(60), _catalog.Username, _catalog.Password, RuleCatalogAuthenticationType.BuiltIn))
            {
                foreach (var ruleApp in catCon.GetAllRuleApps())
                {
                    var ruleAppDefInfo = ruleApp.Key;
                    var ruleAppInfo = ruleApp.Value;

                    if (ruleAppDefInfo.IsLatest)
                    {
                        Console.WriteLine($"Searching Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision} {ruleAppInfo.LastLabelName}");

                        var ruleAppRef = new CatalogRuleApplicationReference(_catalog.Url, ruleAppDefInfo.Name, _catalog.Username, _catalog.Password, ruleAppDefInfo.PublicRevision);
                        var ruleAppDef = ruleAppRef.GetRuleApplicationDef();
                        foreach (var entity in ((IEnumerable<EntityDef>)ruleAppDef.Entities))
                        {
                            foreach (var ruleSet in entity.GetAllRuleSets())
                            {
                                switch (field)
                                {
                                    case SearchField.Description:
                                        if (ruleSet.Comments.Contains(searchQuery))
                                            results.Add($"Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision}, RuleSet {ruleSet.Name} contains Description: {ruleSet.Comments}");
                                        break;
                                    case SearchField.Name:
                                        if (ruleSet.Name.Contains(searchQuery))
                                            results.Add($"Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision} contains RuleSet named {ruleSet.Name}");
                                        break;
                                    case SearchField.Note:
                                        var matchingNote = ruleSet.Notes.FirstOrDefault(n => n.Text.Contains(searchQuery));
                                        if (matchingNote != null)
                                            results.Add($"Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision}, RuleSet {ruleSet.Name} contains Note: {matchingNote.Text}");
                                        break;
                                }

                                foreach (RuleRepositoryDefBase rule in ruleSet.GetAllRuleElements())
                                {
                                    switch (field)
                                    {
                                        case SearchField.Description:
                                            if (rule.Comments.Contains(searchQuery))
                                                results.Add($"Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision}, RuleSet {ruleSet.AuthoringElementPath}, Rule {rule.AuthoringElementPath} contains Description: {rule.Comments}");
                                            break;
                                        case SearchField.Name:
                                            if (rule.Name.Contains(searchQuery))
                                                results.Add($"Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision}, RuleSet {ruleSet.AuthoringElementPath} contains Rule named {rule.AuthoringElementPath}");
                                            break;
                                        case SearchField.Note:
                                            var matchingNote = rule.Notes.FirstOrDefault(n => n.Text.Contains(searchQuery));
                                            if (matchingNote != null)
                                                results.Add($"Rule App {ruleAppDefInfo.Name} v{ruleAppDefInfo.PublicRevision}, RuleSet {ruleSet.AuthoringElementPath}, Rule {rule.AuthoringElementPath} contains Note: {matchingNote.Text}");
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }
        public enum SearchField
        {
            Description,
            Name,
            Note
        }
        #endregion

        #region Automation, CI/CD, and Singles
        private void BuildAndRunInMemoryRuleApp()
        {
            var ruleApp = new RuleApplicationDef("MyAwesomeInMemoryRuleApp");
            ruleApp.Entities = new EntityDefCollection();

            var additionEntity = new EntityDef("AdditionProblem");
            additionEntity.RuleElements = new RuleElementDefCollection();
            ruleApp.Entities.Add(additionEntity);

            additionEntity.Fields = new FieldDefCollection();
            additionEntity.Fields.Add(new FieldDef("FactorA", DataType.Integer));
            additionEntity.Fields.Add(new FieldDef("FactorB", DataType.Integer));
            additionEntity.Fields.Add(new FieldDef("Result", DataType.Integer));

            var additionRuleSet = new RuleSetDef("AddFactors") { FireMode = RuleSetFireMode.Auto, RunMode = RuleSetRunMode.SequentialRunOnce };
            additionRuleSet.Rules = new RuleElementDefCollection();
            additionEntity.RuleElements.Add(additionRuleSet);

            var addNumbersAction = new SetValueActionDef("Result", "FactorA+FactorB") { Name = "AddNumbersTogether" };
            additionRuleSet.Rules.Add(addNumbersAction);

            var languageRule = new LanguageRuleDef("TestIfDate") { IncludeInBusinessLanguage=true, AuthoringUseNameForElementTreeName=true };
            additionEntity.RuleElements.Add(languageRule);
            var ifThen = new CalcDef("IsDate(Field1)", FormulaParseFormat.Formula);
            //languageRule.RuleElement = ifThen;

            var ruleAppRef = new InMemoryRuleApplicationReference(ruleApp);
            using (var session = new RuleSession(ruleApplicationReference: ruleAppRef))
            {
                session.Settings.LogOptions = EngineLogOptions.SummaryStatistics | EngineLogOptions.DetailStatistics;

                var problemEntity = session.CreateEntity("AdditionProblem", "{ 'FactorA': 19243, 'FactorB': 53423 }", EntityStateType.Json);
                session.ApplyRules();
                Console.WriteLine(problemEntity.GetJson());
            }
        }

        private RuleApplicationDef GetRuleAppWithUpdatedSettings()
        {
            var ruleApp = GetCatalogRuleApp("UpdateAppSample").GetRuleApplicationDef();

            #region Retrieve Data
            Dictionary<string, string> newInlineValueListData = new Dictionary<string, string>() {
                { "NewValue1", "NewDisplayName1" },
                { "NewValue2", null }
            };
            object[,] newInlineTableData = {
                { "NewName1", "NewFirstValue", "NewSecondValue" },
                { "NewName2", "NewerFirstValue", "NewerSecondValue" }
            };
            #endregion

            ruleApp.UpdateDbConnectionString("DatabaseConnection1", "NewConnectionString");
            ruleApp.UpdateRestRootUrl("RestService1", "NewBaseUrl");
            ruleApp.UpdateRestOperationHeader("RestOperation1", "Authorization", "APIKEY NewApiTokenValue");
            ruleApp.UpdateFieldDefaultValue("Entity1", "Field1", "NewDefaultValue");
            ruleApp.UpdateInlineValueList("InlineValueList1", newInlineValueListData);
            ruleApp.UpdateInlineTable("InlineTable1", newInlineTableData);

            //ruleApp.SaveToFile($"UpdateAppSample_Updated.ruleappx");
            // TODO: Promote Rule App

            return ruleApp;
        }

        public void RunTestSuite(string ruleAppFilePath, string testSuiteFilePath)
        {
            var ruleApp = new FileSystemRuleApplicationReference(ruleAppFilePath);
            var ruleAppDef = ruleApp.GetRuleApplicationDef();

            var suite = TestSuiteDef.LoadFrom(new ZipFileTestSuitePersistenceProvider(testSuiteFilePath));
            suite.ActiveRuleApplicationDef = ruleAppDef;
        
            TestResultCollection results;
            using (TestingSessionManager manager = new TestingSessionManager(new InProcessConnectionFactory()))
            {
                var session = new RegressionTestingSession(manager, suite);
                results = session.ExecuteAllTests();
            }

            foreach(var result in results)
            {
                if (result.Passed)
                    Console.WriteLine($"Test passed: {result.TestDef.DisplayName}");
                else
                {
                    Console.WriteLine($"Test failed: {result.TestDef.DisplayName}");
                    foreach (var failedAssertionResult in result.AssertionResults.Where(ar => ar.Passed == false))
                    {
                        Console.WriteLine($"    {failedAssertionResult.Target} was {failedAssertionResult.ActualValue}, expected value {failedAssertionResult.ExpectedValue}");
                    }
                }

            }
        }
        private void PromoteRuleApp(string ruleAppName, string label, string destinationCatUrl, string destinationCatUser, string destinationCatPass)
        {
            var sourceRuleApp = new CatalogRuleApplicationReference(_catalog.Url, ruleAppName, _catalog.Username, _catalog.Password, label);
            var sourceRuleAppDef = sourceRuleApp.GetRuleApplicationDef();

            var destCatCon = new RuleCatalogConnection(new Uri(destinationCatUrl), TimeSpan.FromSeconds(60), destinationCatUser, destinationCatPass, RuleCatalogAuthenticationType.BuiltIn);
            var newRuleAppDef = destCatCon.PromoteRuleApplication(sourceRuleAppDef, "Comment");
            destCatCon.ApplyLabel(newRuleAppDef, "LIVE");
        }

        public void RetrieveIrJSFromDistributionService()
        {
            Console.WriteLine();
            Console.WriteLine("Requesting compiled JS library from Distribution Service...");
            var ruleApp = new CatalogRuleApplicationReference(_catalog.Url, "SalesforceRuleApp", _catalog.Username, _catalog.Password, "LIVE");
            var ruleAppDef = ruleApp.GetRuleApplicationDef();
            var js = CallDistributionServiceAsync(ruleAppDef, "https://api.distribution.inrule.com/", ConfigurationManager.AppSettings["irDistributionKey"]).Result;
            Console.WriteLine("Compiled Javascript Rule App:");
            Console.WriteLine(js);
            Console.WriteLine();
        }
        public static async Task<string> CallDistributionServiceAsync(RuleApplicationDef ruleApplication, string serviceUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            using (var requestContent = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(serviceUri);

                // Build up our request by reading in the rule application
                var httpContent = new
                ByteArrayContent(Encoding.UTF8.GetBytes(ruleApplication.GetXml()));
                requestContent.Add(httpContent, "ruleApplication", ruleApplication.Name + ".ruleapp");

                // Tell the server we are sending form data
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                client.DefaultRequestHeaders.Add("subscription-key", subscriptionKey);

                // Post the rule application to the irDistribution service API,
                // enabling Execution and State Change logging, Display Name metadata, and the developer example.
                var distributionUrl = "package?logOption=Execution&logOption=StateChanges&subscriptionkey=" + subscriptionKey;
                var result = await client.PostAsync(distributionUrl, requestContent).ConfigureAwait(false);

                // Get the return package from the result
                dynamic returnPackage = result.Content.ReadAsAsync<JObject>().Result;
                var errors = new StringBuilder();
                if (returnPackage.Status.ToString() == "Fail")
                {
                    foreach (var error in returnPackage.Errors)
                    {
                        // Handle errors
                        errors.AppendLine("* " + error.Description.ToString());
                    }
                    foreach (var unsupportedError in returnPackage.UnsupportedFeatures)
                    {
                        // Handle errors
                        errors.AppendLine("* " + unsupportedError.Feature.ToString());
                    }
                    // Still need to stop processing
                    return errors.ToString();
                }

                // Build the download url of the file
                var downloadUrl = returnPackage.PackagedApplicationDownloadUrl.ToString();

                // Get the contents
                HttpResponseMessage resultDownload = await client.GetAsync(downloadUrl).ConfigureAwait(false);
                if (!resultDownload.IsSuccessStatusCode)
                {
                    // Handle errors
                    errors.AppendLine(resultDownload.Content.ReadAsStringAsync().Result);
                    return errors.ToString();
                }
                return resultDownload.Content.ReadAsStringAsync().Result;
            }
        }
        private void ExecuteWithMetricsLogged()
        {
            try
            {
                var ruleApp = new CatalogRuleApplicationReference(_catalog.Url, "MortgageCalculator", _catalog.Username, _catalog.Password, "LIVE");
                using (var session = new RuleSession(ruleApp))
                {
                    session.Settings.MetricLogger = new ConsoleMetricLogger();

                    var entityState = session.CreateEntity("Mortgage", "{'LoanInfo': { 'Principal' : 400000, 'APR' : 3.1, 'TermInYears' : 30 }}", EntityStateType.Json);
                    entityState.ExecuteRuleSet("PaymentSummaryRuleController");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        #endregion

        #region Decision Services
        private async Task<bool> BuildAndPublishDecision()
        {
            var ruleApplication = new RuleApplicationDef("InterestRateApp");

            // Define 'CalculateLoanPayment' decision
            var calculateLoanPayment = ruleApplication.Decisions.Add(new DecisionDef("CalculateLoanPayment"));
            {
                var presentValue = calculateLoanPayment.Fields.Add(new DecisionFieldDef("PresentValue", DecisionFieldType.Input, DataType.Number));
                var interestRate = calculateLoanPayment.Fields.Add(new DecisionFieldDef("InterestRate", DecisionFieldType.Input, DataType.Number));
                var periods = calculateLoanPayment.Fields.Add(new DecisionFieldDef("Periods", DecisionFieldType.Input, DataType.Integer));
                var payment = calculateLoanPayment.Fields.Add(new DecisionFieldDef("Payment", DecisionFieldType.Output, DataType.Number));
                calculateLoanPayment.DecisionStart.Rules.Add(new SetValueActionDef(payment.Name, $"{interestRate.Name} * {presentValue.Name} / (1 - ((1 + {interestRate.Name}) ^ -{periods.Name}))"));
            }

            // Define 'CalculateFutureValue' decision
            var calculateFutureValue = ruleApplication.Decisions.Add(new DecisionDef("CalculateFutureValue"));
            {
                var presentValue = calculateFutureValue.Fields.Add(new DecisionFieldDef("PresentValue", DecisionFieldType.Input, DataType.Number));
                var interestRate = calculateFutureValue.Fields.Add(new DecisionFieldDef("InterestRate", DecisionFieldType.Input, DataType.Number));
                var periods = calculateFutureValue.Fields.Add(new DecisionFieldDef("Periods", DecisionFieldType.Input, DataType.Integer));
                var futureValue = calculateFutureValue.Fields.Add(new DecisionFieldDef("FutureValue", DecisionFieldType.Output, DataType.Number));
                calculateFutureValue.DecisionStart.Rules.Add(new SetValueActionDef(futureValue.Name, $"{presentValue.Name} * (1 + {interestRate.Name}) ^ {periods.Name}"));
            }

            var result = true;
            result = result && await DecisionServiceClient.PublishDecision(calculateLoanPayment.Name, ruleApplication);
            result = result && await DecisionServiceClient.PublishDecision(calculateFutureValue.Name, ruleApplication);

            return result;
        }
        private async Task<string> ExecuteDecision(string decisionName, string inputJsonState)
        {
            string result;
            result = await DecisionServiceClient.ExecuteDecisionService(decisionName, inputJsonState);
            return result;
        }
        #endregion

        #region Execution Demo Methods
        private async Task<MultiplicationProblem> ApplyRulesViaRex_Denormalized(MultiplicationProblem problem)
        {
            string OOBRuleServiceUrl = ConfigurationManager.AppSettings["RexUrl"] + "/HttpService.svc/";

            RuleRequest request = new ApplyRulesRequest()
            {
                RuleApp = new Ruleapp { RepositoryRuleAppRevisionSpec = new Repositoryruleapprevisionspec() { RuleApplicationName = "MultiplicationApp" } },
                EntityName = "MultiplicationProblem",
                EntityState = JsonConvert.SerializeObject(problem)
            };

            string responseString = "";
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(OOBRuleServiceUrl + request.Route, content);
                responseString = await response.Content.ReadAsStringAsync();
            }

            RuleExecutionResponse responseObject = JsonConvert.DeserializeObject<RuleExecutionResponse>(responseString);
            var finalEntityState = JsonConvert.DeserializeObject<MultiplicationProblem>(responseObject.EntityState);

            return finalEntityState;
        }

        private MultiplicationProblem ApplyRulesViaIrSDK_Denormalized(IrCatalogConnectionSettings catalog, MultiplicationProblem initialEntityState)
        {
            try
            {
                RuleApplicationReference ruleAppRef = GetCatalogRuleApp("MultiplicationApp");

                using (var session = new RuleSession(ruleAppRef))
                {
                    #region Examples of Overrides
                    //Override REST endpoint root URL
                    /*
                    var endpoints = session.RuleApplication.GetRuleApplicationDef().EndPoints;
                    if (endpoints.Contains("ExchangeRateService") && endpoints["ExchangeRateService"].EndPointType == InRule.Repository.EndPoints.EndPointType.RestService)
                        session.Overrides.OverrideRestServiceRootUrl("ExchangeRateService", "https://api.exchangeratesapi.io");
                    */

                    //Override Inline Table
                    /*
                    DataSet newDataSet = new DataSet();
                    //<?xml version="1.0" standalone="yes"?><NewDataSet><Table1><ID>1</ID><Name>Alan Override</Name></Table1><Table1><ID>2</ID><Name>Condiments</Name></Table1></NewDataSet>
                    newDataSet.ReadXml(@"DataTable.xml");
                    var dataElements = session.RuleApplication.GetRuleApplicationDef().DataElements;
                    if(dataElements.Contains("InlineTable1") && dataElements["InlineTable1"] is TableDef)
                        session.Overrides.OverrideTable("InlineTable1", newDataSet.Tables["Table1"]);
                    */
                    #endregion

                    Entity entity = session.CreateEntity("MultiplicationProblem", initialEntityState);

                    session.ApplyRules();

                    foreach (var notification in session.GetNotifications())
                        Console.WriteLine($"Notification {notification.Type}: {notification.Message}");
                }
                return initialEntityState;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public double ExecuteDecisionViaIrSDK_Denormalized(int principal, int apr, int termInYears)
        {
            try
            {
                RuleApplicationReference ruleAppRef = GetCatalogRuleApp("MortgageCalculator");

                using (var session = new RuleSession(ruleAppRef))
                {
                    //Example 1 : JSON string
                    var jsonDecision = session.CreateDecision("MortgageSummary");
                    string inputJson = $"{{ 'Principal': '{principal}', 'APR': '{apr}', 'TermInYears': '{termInYears}',  }}";
                    DecisionResult jsonResult = jsonDecision.Execute(inputJson, EntityStateType.Json);

                    //Example 2 : Embedded Inputs
                    var embeddedDecision = session.CreateDecision("MortgageSummary");
                    DecisionResult embededResult = embeddedDecision.Execute(new DecisionInput("Principal", 500000), new DecisionInput("APR", 3), new DecisionInput("TermInYears", 30));
                    
                    //Example 3 : Explicitly Created Input Entity
                    var info = session.CreateEntity("LoanInfo");
                    info.Fields["Principal"].SetValue(principal);
                    info.Fields["APR"].SetValue(apr);
                    info.Fields["TermInYears"].SetValue(termInYears);
                    var entityDecision = session.CreateDecision("MortgageSummaryFromEntity");
                    DecisionResult entityResult = entityDecision.Execute(new DecisionInput("Info", info));

                    foreach (var notification in session.GetNotifications())
                        Console.WriteLine($"Notification {notification.Type}: {notification.Message}");

                    var output = JsonConvert.DeserializeObject<dynamic>(jsonResult.ToJson());
                    return (double)(output.Summary.MonthlyPayment);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 0;
            }
        }

        private class MultiplicationProblem
        {
            public double FactorA;
            public double FactorB;
            public double Result;
        }

        #endregion

        #region Execution Helper Methods
        private double IrSDKApplyMultiplication(double factorA, double factorB, bool provideFeedback = true, bool addLogOptions = false)
        {
            if(provideFeedback) Console.WriteLine("Performing irSDK Apply Rule");
            try
            {
                var problem = new MultiplicationProblem()
                {
                    FactorA = factorA,
                    FactorB = factorB
                };

                var logOptions = EngineLogOptions.None;
                if (addLogOptions)
                    logOptions = EngineLogOptions.SummaryStatistics | EngineLogOptions.DetailStatistics;

                IrSDKClient.InvokeEngine(_catalog, "MultiplicationApp", "MultiplicationProblem", problem, engineLogOptions: logOptions, log: false);

                return problem.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }
        private string IrSDKApplyJson(string ruleApp, string entityName, string entityState)
        {
            return IrSDKClient.InvokeEngine(_catalog, ruleApp, entityName, entityState, log: false);
        }
        private RuleApplicationReference GetCatalogRuleApp(string ruleAppName)
        {
            return new CatalogRuleApplicationReference(_catalog.Url, ruleAppName, _catalog.Username, _catalog.Password);

        }
        #endregion

        #region Self-Hosted IService Implementation
        public double Multiply(double factorA, double factorB)
        {
            return IrSDKApplyMultiplication(factorA, factorB);
        }
        public double MultiplyWithRounding(double factorA, double factorB)
        {
            return IrSDKExecute(factorA, factorB);
        }

        private double IrSDKExecute(double factorA, double factorB)
        {
            Console.WriteLine("Performing irSDK Execute Rule");
            try
            {
                var ruleAppRef = GetCatalogRuleApp("MultiplicationApp");
                var problem = new MultiplicationProblem()
                {
                    FactorA = factorA,
                    FactorB = factorB
                };
                using (var session = new RuleSession(ruleApplicationReference: ruleAppRef))
                {
                    var problemEntity = session.CreateEntity("MultiplicationProblem", problem);
                    problemEntity.ExecuteRuleSet("MultiplyAfterRounding");
                    Console.WriteLine(problem.ToString());
                }
                return problem.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }
        #endregion
    }
}