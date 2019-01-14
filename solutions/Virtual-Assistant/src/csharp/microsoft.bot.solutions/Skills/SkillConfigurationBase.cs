using System.Collections.Generic;
using Microsoft.Bot.Builder.Azure;

namespace Microsoft.Bot.Solutions.Skills
{
    public abstract class SkillConfigurationBase
    {
        public bool IsAuthenticatedSkill { get; set; }

        public abstract Dictionary<string, string> AuthenticationConnections { get; set; }

        public abstract CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public abstract Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; }

        public abstract Dictionary<string, object> Properties { get; set; }
    }
}