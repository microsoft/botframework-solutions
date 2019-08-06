using Microsoft.Bot.Builder.Solutions.Responses;

namespace ITSMSkill.Models
{
    public class KnowledgeCard : ICardData
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string UpdatedTime { get; set; }

        public string Content { get; set; }

        public string Speak { get; set; }
    }
}
