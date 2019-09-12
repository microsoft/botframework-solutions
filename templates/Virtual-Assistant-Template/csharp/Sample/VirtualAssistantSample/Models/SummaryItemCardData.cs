using Microsoft.Bot.Builder.Solutions.Responses;

namespace VirtualAssistantSample.Models
{
    public class SummaryItemCardData : ICardData
    {
        public string Head { get; set; }

        public string HeadColor { get; set; }

        public string Title { get; set; }

        public string Indicator { get; set; }

        public bool IsSubtle { get; set; }
    }
}
