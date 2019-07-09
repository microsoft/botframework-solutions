namespace CalendarSkill.Models
{
    public class CalendarSkillDialogOptions
    {
        public CalendarSkillDialogOptions()
        {

        }

        public CalendarSkillDialogOptions(object options)
        {
            var skillOptions = options as CalendarSkillDialogOptions;
            if (skillOptions != null)
            {
                SubFlowMode = skillOptions.SubFlowMode;
                DialogState = skillOptions.DialogState;
            }
        }

        public bool SubFlowMode { get; set; }

        public CalendarDialogStateBase DialogState { get; set; } = null;
    }
}