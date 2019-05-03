using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Models
{
    public class CalendarDetailCardData : ICardData
    {
        public string Content { get; set; }

        public string MeetingLink { get; set; }
    }
}
