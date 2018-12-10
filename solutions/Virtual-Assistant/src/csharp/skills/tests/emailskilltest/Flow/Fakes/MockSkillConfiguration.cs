using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockSkillConfiguration : ISkillConfiguration
    {
        public MockSkillConfiguration()
        {
            this.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                LuisServices = new Dictionary<string, IRecognizer>
                {
                    { "general", new MockGeneralLuisRecognizer() },
                    { "email", new MockEmailLuisRecognizer() }
                }
            });

            this.AuthenticationConnections.Add("Google", "Google");

            this.TelemetryClient = null;
            this.CosmosDbOptions = null;
        }

        public override TelemetryClient TelemetryClient { get; set; }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();
    }
}
