using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions;

namespace CalendarSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
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