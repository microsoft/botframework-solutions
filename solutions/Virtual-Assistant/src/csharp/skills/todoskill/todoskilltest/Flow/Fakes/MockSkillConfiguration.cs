using System.Collections.Generic;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Solutions.Skills;

namespace ToDoSkillTest.Flow.Fakes
{
    public class MockSkillConfiguration : SkillConfigurationBase
    {
        public MockSkillConfiguration()
        {
            this.AuthenticationConnections = new Dictionary<string, string>();
            this.AuthenticationConnections.Add("Microsoft", "Microsoft");

            this.CosmosDbOptions = null;
        }

        public override CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public override Dictionary<string, LocaleConfiguration> LocaleConfigurations { get; set; } = new Dictionary<string, LocaleConfiguration>();

        public override Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override Dictionary<string, string> AuthenticationConnections { get; set; } = new Dictionary<string, string>();
    }
}