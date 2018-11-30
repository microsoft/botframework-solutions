using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Skills;

namespace AutomotiveSkillTest.Flow.Fakes
{
    public class MockSkillConfiguration : ISkillConfiguration
    {
        public MockSkillConfiguration()
        {
            this.LuisServices.Add("general", new MockLuisRecognizer());
            this.LuisServices.Add("vehiclesettings", new MockLuisRecognizer());
            this.LuisServices.Add("vehiclesettings_name_selection", new MockLuisRecognizer());
            this.LuisServices.Add("vehiclesettings_value_selection", new MockLuisRecognizer());

            this.TelemetryClient = null;
            this.CosmosDbOptions = null;
        }

        public override TelemetryClient TelemetryClient { get; set; }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();
    }
}
