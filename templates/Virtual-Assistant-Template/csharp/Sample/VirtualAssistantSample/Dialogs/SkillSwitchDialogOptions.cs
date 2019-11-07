using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace VirtualAssistantSample.Dialogs
{
    public class SkillSwitchDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkillSwitchDialogOptions"/> class.
        /// </summary>
        /// <param name="prompt">The <see cref="Activity"/> to display when prompting to switch skills.</param>
        /// <param name="manifest">The <see cref="SkillManifest"/> for the new skill.</param>
        public SkillSwitchDialogOptions(Activity prompt, SkillManifest manifest)
        {
            Prompt = prompt;
            Skill = manifest;
        }

        public SkillManifest Skill { get; set; }

        public Activity Prompt { get; set; }
    }
}
