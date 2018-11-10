using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Skills
{
    public abstract class ISkillConfiguration
    {
        public bool IsAuthenticatedSkill { get; set; }

        public abstract Dictionary<string, string> AuthenticationConnections { get; set; }

        public abstract TelemetryClient TelemetryClient { get; set; }

        public abstract CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public abstract Dictionary<string, IRecognizer> LuisServices { get; set; }

        public abstract Dictionary<string, object> Properties { get; set; }
    }
}
