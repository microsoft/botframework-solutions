using Microsoft.Bot.Builder.Solutions.Responses;

namespace VirtualAssistantSample.Models
{
    public class SummaryCardData : ICardData
    {
        public string Title { get; set; }

        public string TotalEventKinds { get; set; }

        public string TotalEventCount { get; set; }

        public string TotalEventKindUnit { get; set; }

        public string TotalEventCountUnit { get; set; }

        public string Provider { get; set; }

        public string UserPhoto { get; set; }

        public string Indicator { get; set; }

    }
}
