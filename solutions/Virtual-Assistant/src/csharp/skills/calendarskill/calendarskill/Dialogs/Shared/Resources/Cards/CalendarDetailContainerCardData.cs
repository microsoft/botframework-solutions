using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Dialogs.Shared.Resources.Cards
{
    public class CalendarDetailContainerCardData : ICardData
    {
        public string ParticipantPhoto1 { get; set; }

        public string ParticipantPhoto2 { get; set; }

        public string ParticipantPhoto3 { get; set; }

        public string ParticipantPhoto4 { get; set; }

        public string ParticipantPhoto5 { get; set; }

        public int OmittedParticipantCount { get; set; }

        public string Title { get; set; }

        public string Date { get; set; }

        public string Time { get; set; }

        public string Location { get; set; }

        public string LocationIcon { get; set; }

        public string Duration { get; set; }
    }
}
