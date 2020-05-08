namespace IntegrationTrainingSamples.Model
{
    public abstract class RuleRequest
    {
        public Ruleapp RuleApp { get; set; }
        public Ruleengineserviceoutputtypes RuleEngineServiceOutputTypes { get; set; }
        public abstract string Route { get; }
    }
    public abstract class EntityStateRuleRequest : RuleRequest
    {
        public string EntityName { get; set; }
        public string EntityState { get; set; }
    }
    public class Ruleapp
    {
        public string FileName { get; set; }
        public string RepositoryServiceUri { get; set; }
        public Repositoryruleapprevisionspec RepositoryRuleAppRevisionSpec { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    public class Repositoryruleapprevisionspec
    {
        public string Label { get; set; }
        public int Revision { get; set; }
        public string RuleApplicationName { get; set; }
    }
    public class Ruleengineserviceoutputtypes
    {
        public bool ActiveNotifications { get; set; }
        public bool ActiveValidations { get; set; }
        public bool EntityState { get; set; }
        public bool Overrides { get; set; }
        public bool RuleExecutionLog { get; set; }
    }

    public class ApplyRulesRequest : EntityStateRuleRequest
    {
        public override string Route { get { return "ApplyRules"; } }
    }
    public class ExecuteRuleSetRequest : EntityStateRuleRequest
    {
        public override string Route { get { return "ExecuteRuleSet"; } }
        public Parameter[] Parameters { get; set; }
        public string RuleSetName { get; set; }
    }
    public class Parameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public class ExecuteDecisionRequest : RuleRequest
    {
        public override string Route { get { return "ExecuteDecision"; } }

        public string DecisionName { get; set; }
        public string InputState { get; set; }
    }

    public class ExecutionResponse
    {
        public Activenotification[] ActiveNotifications { get; set; }
        public Activevalidation[] ActiveValidations { get; set; }
        public bool HasRuntimeErrors { get; set; }
        public Ruleexecutionlog RuleExecutionLog { get; set; }
    }
    public class RuleExecutionResponse : ExecutionResponse
    {
        public string EntityState { get; set; }
    }
    public class DecisionExecutionResponse : ExecutionResponse
    {
        public string OutputState { get; set; }
    }
    public class Activenotification
    {
        public string ElementId { get; set; }
        public bool IsActive { get; set; }
        public string Message { get; set; }
        public string NotificationType { get; set; }
    }
    public class Activevalidation
    {
        public string ElementIdentifier { get; set; }
        public string InvalidMessageText { get; set; }
        public bool IsValid { get; set; }
        public Reason[] Reasons { get; set; }
    }
    public class Reason
    {
        public string FiringRuleId { get; set; }
        public string MessageText { get; set; }
    }
    public class Ruleexecutionlog
    {
        public Message[] Messages { get; set; }
        public long TotalEvaluationCycles { get; set; }
        public string TotalExecutionTime { get; set; }
        public int TotalTraceFrames { get; set; }
    }
    public class Message
    {
        public string Description { get; set; }
        public int ChangeType { get; set; }
        public int CollectionCount { get; set; }
        public string CollectionId { get; set; }
        public string MemberId { get; set; }
        public int MemberIndex { get; set; }
    }
}
