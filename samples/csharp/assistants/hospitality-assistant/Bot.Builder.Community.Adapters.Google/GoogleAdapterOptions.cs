using Bot.Builder.Community.Adapters.Google.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Builder.Community.Adapters.Google
{
    public class GoogleAdapterOptions
    {
        public GoogleWebhookType WebhookType { get; set; } = GoogleWebhookType.Conversation;

        public bool ShouldEndSessionByDefault { get; set; } = true;

        public bool TryConvertFirstActivityAttachmentToGoogleCard { get; set; }

        public string ActionInvocationName { get; set; }

        public string ActionProjectId { get; set; }

        public MultipleOutgoingActivitiesPolicies MultipleOutgoingActivitiesPolicy { get; set; } = MultipleOutgoingActivitiesPolicies.TakeLastActivity;

        public bool ValidateIncomingRequests { get; internal set; }
    }

    public enum MultipleOutgoingActivitiesPolicies
    {
        TakeFirstActivity,
        TakeLastActivity,
        ConcatenateTextSpeakPropertiesFromAllActivities
    }
}
