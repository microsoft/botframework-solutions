using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions;

namespace EmailSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public DefaultValueConfiguration DefaultValue { get; set; }

        public class DefaultValueConfiguration
        {
            public List<SlotFillingConfigItem> SendEmail { get; set; }

            public class SlotFillingConfigItem
            {
                public string Name { get; set; }

                public string DefaultValue { get; set; }

                public bool IsSkipByDefault { get; set; }
            }
        }
    }
}