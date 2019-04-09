using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Models
{
    public class CalendarDetailCardData : ICardData
    {
        public string Title { get; set; }

        public string DateTime { get; set; }

        public string Location { get; set; }

        public string Content { get; set; }

        public string MeetingLink { get; set; }

        public string LocationIcon { get; set; }
    }
}
