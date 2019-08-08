using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillSwitchConfirmOption
    {
        public Activity FallbackHandledEvent { get; set; }

        public string TargetIntent { get; set; }

        public Activity UserInputActivity { get; set; }
    }
}
