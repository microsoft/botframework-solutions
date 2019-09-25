using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class RoomItem : ICardData
    {
        public string Item { get; set; }

        public string[] Names { get; set; }

        public int Quantity { get; set; }
    }
}
