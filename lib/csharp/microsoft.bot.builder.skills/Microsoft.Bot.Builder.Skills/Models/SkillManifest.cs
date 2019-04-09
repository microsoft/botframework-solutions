using System;

namespace Microsoft.Bot.Builder.Skills.Models
{
    public class SkillManifest
    {
        public string id { get; set; }
        public string name { get; set; }
        public Uri endpoint { get; set; }
        public string description { get; set; }
        public string suggestedAction { get; set; }
        public Uri iconUrl { get; set; }
        public Authenticationconnection[] authenticationConnections { get; set; }
        public Action[] actions { get; set; }
    }

    public class Authenticationconnection
    {
        public string id { get; set; }
        public string serviceProviderId { get; set; }
        public string scopes { get; set; }
    }

    public class Action
    {
        public string id { get; set; }
        public Definition definition { get; set; }
    }

    public class Definition
    {
        public string description { get; set; }
        public Slot[] slots { get; set; }
        public Triggers triggers { get; set; }
    }

    public class Triggers
    {
        public Utterance[] utterances { get; set; }
        public Event[] events { get; set; }
    }

    public class Utterance
    {
        public string locale { get; set; }
        public string[] source { get; set; }
    }

    public class Event
    {
        public string Name { get; set; }
    }

    public class Slot
    {
        public string name { get; set; }
        public string[] types { get; set; }
    }
}
