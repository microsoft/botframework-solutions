using Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
using System.Collections.Generic;

namespace SkillTemplateTest.Dialogs.Fakes
{
    public class MockSkillConfiguration : SkillConfigurationBase
    {
        public MockSkillConfiguration()
        {
            LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", new MockLuisRecognizer(defaultIntent: (General.Intent.None, 1.0)) },
                    { "skill", new MockLuisRecognizer(defaultIntent: (Skill.Intent.None, 1.0)) }
                }
            });

            CosmosDbOptions = null;
        }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();
    }
}