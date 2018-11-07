using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Skills;
using Moq;

namespace EmailSkillTest.API.Fakes
{
    public class MockSkillConfiguration : ISkillConfiguration
    {
        public MockSkillConfiguration()
        {
            this.TelemetryClient = null;
            this.CosmosDbOptions = null;

            this.AuthConnectionName = "test";
        }

        public override string AuthConnectionName { get; set; }

        public override TelemetryClient TelemetryClient { get; set; }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
