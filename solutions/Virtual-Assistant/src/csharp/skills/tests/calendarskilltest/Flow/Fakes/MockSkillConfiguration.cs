using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Skills;
using Moq;
using CalendarSkillTest.Flow.Utterances;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockSkillConfiguration : SkillConfiguration
    {
        public MockSkillConfiguration()
        {
            this.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, IRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer() }
                }
            });

            this.AuthenticationConnections = new Dictionary<string, string>();
            this.AuthenticationConnections.Add("Microsoft", "Microsoft");

            this.TelemetryClient = null;
            this.CosmosDbOptions = null;
        }

        public override Dictionary<string, string> AuthenticationConnections { get; set; }

        public override TelemetryClient TelemetryClient { get; set; }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
