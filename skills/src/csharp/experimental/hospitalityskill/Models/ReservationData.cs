using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class ReservationData : ICardData
    {
        public string CheckInDate { get; set; }

        public string CheckOutDate { get; set; }

        public string CheckOutTime { get; set; }
    }
}
