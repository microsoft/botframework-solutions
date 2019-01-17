using Microsoft.Bot.Solutions.Cards;

namespace CalendarSkill
{
    public class CalendarCardData : CardDataBase
    {
        public string Title { get; set; }

        public string Participant { get; set; }

        public string Date { get; set; }

        public string Time { get; set; }

        public string Location { get; set; }

        public string ContentPreview { get; set; }

        public string MeetingLink { get; set; }

        public string Speak { get; set; }
    }
}
