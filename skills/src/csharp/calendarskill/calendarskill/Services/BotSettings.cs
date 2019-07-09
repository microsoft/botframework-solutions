using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions;

namespace CalendarSkill.Services
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
            public List<SlotFillingConfigItem> CreateMeeting { get; set; }

            public class SlotFillingConfigItem
            {
                public string Name { get; set; }

                public bool IsSkipByDefault { get; set; }

                public string DefaultValue { get; set; }
            }
        }
    }
}