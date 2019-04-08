using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Dialogs.Shared.Resources.Cards
{
    public class CalendarMeetingListCardData : ICardData
    {
        public string ListTitle { get; set; }

        public string TotalEventCount { get; set; }

        public string OverlapEventCount { get; set; }

        public string TotalEventCountUnit { get; set; }

        public string OverlapEventCountUnit { get; set; }

        public string Provider { get; set; }

        public string UserPhoto { get; set; }
    }
}
