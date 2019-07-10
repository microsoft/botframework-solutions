using Microsoft.Bot.Builder.Solutions;

namespace ToDoSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public int DisplaySize { get; set; }

        public string TaskServiceProvider { get; set; }
    }
}