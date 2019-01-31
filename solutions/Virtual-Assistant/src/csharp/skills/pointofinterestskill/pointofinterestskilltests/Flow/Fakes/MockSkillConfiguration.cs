using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkillTests.API.Fakes;
using System;
using System.Collections.Generic;
using System.Text;

namespace PointOfInterestSkillTests.Flow.Fakes
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
                    { "general", new MockLuisRecognizer() },
                    { "pointofinterest", new MockLuisRecognizer() }
                },
            });

            this.CosmosDbOptions = null;

            this.Properties.Add("AzureMapsKey", MockData.Key);
        }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();
    }
}
