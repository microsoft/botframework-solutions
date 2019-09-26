using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class MenuItem : ICardData
    {
        public string Name { get; set; }

        public string[] AllNames { get; set; }

        public bool GlutenFree { get; set; }

        public bool Vegetarian { get; set; }

        public int Price { get; set; }

        public string Description { get; set; }
    }
}
