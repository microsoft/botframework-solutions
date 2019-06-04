namespace CalendarSkill.Models
{
    public class CalendarSkillDialogOptions
    {
        public bool SubFlowMode { get; set; }

        public CalendarDialogStateBase DialogState { get; set; } = null;
    }
}