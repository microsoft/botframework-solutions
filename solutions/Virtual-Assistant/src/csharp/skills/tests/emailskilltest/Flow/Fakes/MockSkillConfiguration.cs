using System.Collections.Generic;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockSkillConfiguration : SkillConfigurationBase
    {
        public MockSkillConfiguration()
        {
            this.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", new MockGeneralLuisRecognizer() },
                    { "email", new MockEmailLuisRecognizer() }
                }
            });

            this.AuthenticationConnections.Add("Google", "Google");

            this.CosmosDbOptions = null;
        }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();
    }
}