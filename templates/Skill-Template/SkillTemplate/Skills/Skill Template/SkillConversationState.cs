using Luis;
using Microsoft.Bot.Builder.Dialogs;

namespace $safeprojectname$
{
    public class SkillConversationState : DialogState
    {
        public SkillConversationState()
        {
        }

        public string Token { get; internal set; }
    
        public $safeprojectname$LU LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}