using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Services
{
    // Mock hotel service
    // Should replace with real apis
    public class HotelService : IHotelService
    {
        public HotelService()
        {
        }

        public async Task<string> GetLateCheckOutAsync()
        {
            // make request for the late check out time
            var lateTime = "4:00 pm";

            return await Task.FromResult(lateTime);
        }

        public Task<ReservationData> GetReservationDetails()
        {
            // make request for reservation details
            return Task.FromResult(new ReservationData());
        }

        public void UpdateReservationDetails(ReservationData reservation)
        {
            // make request to update user's reservation details
        }

        public async Task<bool> RequestItems(List<ItemRequestClass> items)
        {
            // send request for this list of items to be brought
            return await Task.FromResult(true);
        }
    }
}
