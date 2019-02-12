using Luis;
using Microsoft.Bot.Builder.Dialogs;

namespace DemoSkill
{
    public class SkillConversationState : DialogState
    {
        public SkillConversationState()
        {
        }

        public string Token { get; internal set; }

        public DemoSkillLU LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}