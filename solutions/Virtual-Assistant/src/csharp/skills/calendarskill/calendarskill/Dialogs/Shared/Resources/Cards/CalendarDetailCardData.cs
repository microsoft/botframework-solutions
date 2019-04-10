using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Dialogs.Shared.Resources.Cards
{
    public class CalendarDetailCardData : ICardData
    {
        public string Content { get; set; }

        public string MeetingLink { get; set; }
    }
}
