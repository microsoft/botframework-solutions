using EmailSkill.Models.DialogModel;

namespace EmailSkill.Models
{
    public class EmailSkillDialogOptions
    {
        public bool SubFlowMode { get; set; } = false;

        public EmailStateBase DialogState { get; set; } = null;
    }
}