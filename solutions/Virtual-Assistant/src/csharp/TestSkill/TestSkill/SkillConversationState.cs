using Microsoft.Bot.Builder.Dialogs;

namespace TestSkill
{
    public class SkillConversationState : DialogState
    {
        public SkillConversationState()
        {
        }

        public string Token { get; internal set; }

        public void Clear()
        {
        }
    }
}