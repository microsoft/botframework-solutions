using System.Threading.Tasks;
using HospitalitySkill.Models;

namespace HospitalitySkill.Services
{
    // Service used to call hotel apis
    public interface IHotelService
    {
        // get reservation details
        Task<ReservationData> GetReservationDetailsAsync();

        // update reservation
        void UpdateReservationDetails(ReservationData reservation);

        // check late check out availability
        Task<string> GetLateCheckOutAsync();
    }
}
