using Microsoft.Bot.Builder.Dialogs;

namespace TestSkill1
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