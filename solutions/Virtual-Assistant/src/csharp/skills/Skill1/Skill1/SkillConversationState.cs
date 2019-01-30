using Luis;
using Microsoft.Bot.Builder.Dialogs;

namespace Skill1
{
    public class SkillConversationState : DialogState
    {
        public SkillConversationState()
        {
        }

        public string Token { get; internal set; }

        public Skill1LU LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}