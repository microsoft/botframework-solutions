using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkillTest.API.Fakes
{
    public class MockSkillConfiguration : ISkillConfiguration
    {
        public MockSkillConfiguration()
        {
            this.TelemetryClient = null;
            this.CosmosDbOptions = null;
            this.AuthenticationConnections = new Dictionary<string, string>
            {
                { "Google", "Google" }
            };
        }

        public override Dictionary<string, string> AuthenticationConnections { get; set; }

        public override TelemetryClient TelemetryClient { get; set; }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
