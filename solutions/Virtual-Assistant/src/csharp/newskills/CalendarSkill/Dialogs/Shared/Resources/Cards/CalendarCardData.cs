using Microsoft.Bot.Solutions.Cards;

namespace CalendarSkill
{
    public class CalendarCardData : CardDataBase
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public string MeetingLink { get; set; }

        public string Speak { get; set; }
    }
}
