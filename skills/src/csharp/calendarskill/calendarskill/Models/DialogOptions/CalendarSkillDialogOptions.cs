namespace CalendarSkill.Models.DialogOptions
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
            }
        }

        public bool SubFlowMode { get; set; }
    }
}