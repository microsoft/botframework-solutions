using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class FoodOrderData : ICardData
    {
        public string Name { get; set; }

        public string SpecialRequest { get; set; }

        public int Price { get; set; }

        public int Quantity { get; set; }

        public int BillTotal { get; set; }
    }
}
