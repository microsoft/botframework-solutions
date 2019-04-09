using Microsoft.Bot.Builder.Solutions.Shared.Responses;

namespace CalendarSkill.Models
{
    public class CalendarDetailContainerCardData : ICardData
    {
        public string ParticipantPhoto1 { get; set; }

        public string ParticipantPhoto2 { get; set; }

        public string ParticipantPhoto3 { get; set; }

        public string ParticipantPhoto4 { get; set; }

        public string ParticipantPhoto5 { get; set; }

        public int OmittedParticipantCount { get; set; }
    }
}