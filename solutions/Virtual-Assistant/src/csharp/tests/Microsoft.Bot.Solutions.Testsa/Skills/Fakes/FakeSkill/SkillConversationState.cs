using Microsoft.Bot.Builder.Dialogs;

namespace FakeSkill
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