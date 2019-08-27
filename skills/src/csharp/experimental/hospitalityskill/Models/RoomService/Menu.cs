using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class Menu : ICardData
    {
        public string Type { get; set; }

        public string TimeAvailable { get; set; }

        public MenuItem[] Items { get; set; }
    }
}
