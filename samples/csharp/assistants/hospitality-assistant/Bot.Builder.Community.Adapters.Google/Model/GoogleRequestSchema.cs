using System;

namespace Bot.Builder.Community.Adapters.Google
{
    public class DialogFlowRequest
    {
        public string ResponseId { get; set; }
        public QueryResult QueryResult { get; set; }
        public OriginalDetectIntentRequest OriginalDetectIntentRequest { get; set; }
        public string Session { get; set; }
    }

    public class QueryResult
    {
        public string QueryText { get; set; }
        public string Action { get; set; }
        public Parameters Parameters { get; set; }
        public bool AllRequiredParamsPresent { get; set; }
        public OutputContext[] OutputContexts { get; set; }
        public Intent Intent { get; set; }
        public double IntentDetectionConfidence { get; set; }
        public DiagnosticInfo DiagnosticInfo { get; set; }
        public string LanguageCode { get; set; }
    }

    public class Parameters
    {
    }

    public class Intent
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

    public class DiagnosticInfo
    {
    }

    public class OutputContext
    {
        public string Name { get; set; }
    }

    public class OriginalDetectIntentRequest
    {
        public string Source { get; set; }
        public string Version { get; set; }
        public ActionsPayload Payload { get; set; }
    }

    public class ActionsPayload
    {
        public bool IsInSandbox { get; set; }
        public Surface Surface { get; set; }
        public Input[] Inputs { get; set; }
        public User User { get; set; }
        public Conversation Conversation { get; set; }
        public AvailableSurface[] AvailableSurfaces { get; set; }
    }

    public class Surface
    {
        public Capability[] Capabilities { get; set; }
    }

    public class Capability
    {
        public string Name { get; set; }
    }

    public class User
    {
        public DateTime LastSeen { get; set; }
        public string Locale { get; set; }
        public string UserId { get; set; }
        public string IdToken { get; set; }
        public string AccessToken { get; set; }
        public string UserStorage { get; set; }
    }

    public class Conversation
    {
        public string ConversationId { get; set; }
        public string Type { get; set; }
        public string ConversationToken { get; set; }
    }

    public class Input
    {
        public RawInput[] RawInputs { get; set; }
        public Argument[] Arguments { get; set; }
        public string Intent { get; set; }
    }

    public class RawInput
    {
        public string Query { get; set; }
        public string InputType { get; set; }
    }

    public class Argument
    {
        public string RawText { get; set; }
        public string TextValue { get; set; }
        public string Name { get; set; }
        public object Extension { get; set; }
    }

    public class AvailableSurface
    {
        public Capability[] Capabilities { get; set; }
    }
}
