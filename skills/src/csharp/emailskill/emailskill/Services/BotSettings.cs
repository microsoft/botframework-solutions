using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions;

namespace EmailSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string GoogleAppName { get; set; }

        public string GoogleClientId { get; set; }

        public string GoogleClientSecret { get; set; }

        public string GoogleScopes { get; set; }

        public int DisplaySize { get; set; }

        public DefaultValueConfiguration DefaultValue { get; set; }

        public class DefaultValueConfiguration
        {
            public List<SlotFillingConfigItem> SendEmail { get; set; }

            public class SlotFillingConfigItem
            {
                public string Name { get; set; }

                public bool IsSkipByDefault { get; set; }
            }
        }
    }
}