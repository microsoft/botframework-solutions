using System.Collections.Generic;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Solutions.Skills;

namespace Microsoft.Bot.Solutions.Testing.Fakes
{
    public class MockSkillConfiguration : SkillConfigurationBase
    {
        public override CosmosDbStorageOptions CosmosDbOptions { get; set; } = null;

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();
    }
}