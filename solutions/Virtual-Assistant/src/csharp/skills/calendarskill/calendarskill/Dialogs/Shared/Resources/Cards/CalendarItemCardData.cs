using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill
{
    public class CalendarItemCardData : ICardData
    {
        public string Time { get; set; }

        public string TimeColor { get; set; }

        public string Title { get; set; }

        public string Location { get; set; }

        public string Duration { get; set; }

        public bool IsSubtle { get; set; }
    }
}
