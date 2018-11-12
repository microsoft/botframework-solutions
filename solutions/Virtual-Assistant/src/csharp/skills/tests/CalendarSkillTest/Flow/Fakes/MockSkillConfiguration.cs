using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Skills;
using Moq;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockSkillConfiguration : SkillConfiguration
    {
        public MockSkillConfiguration()
        {
            this.LuisServices.Add("general", new MockLuisRecognizer());
            this.LuisServices.Add("calendar", new MockLuisRecognizer());
            this.AuthenticationConnections = new Dictionary<string, string>();
            this.AuthenticationConnections.Add("Microsoft", "Microsoft");

            this.TelemetryClient = null;
            this.CosmosDbOptions = null;
        }

        public override Dictionary<string, string> AuthenticationConnections { get; set; }

        public override TelemetryClient TelemetryClient { get; set; }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
