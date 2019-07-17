using System;
using System.Threading.Tasks;
using HospitalitySkill.Models;

namespace HospitalitySkill.Services
{
    // Mock hotel service
    // Should replace with real apis
    public class HotelService : IHotelService
    {
        private ReservationData _reservationData;

        public HotelService()
        {
            // mock data for hotel reservation
            _reservationData = new ReservationData
            {
                CheckInDate = "June 29, 2019",
                CheckOutDate = DateTime.Now.ToString("MMMM d, yyyy"),
                CheckOutTime = "12:00 pm"
            };
        }

        public async Task<string> GetLateCheckOutAsync()
        {
            // make request for the late check out time
            var lateTime = "4:00 pm";

            return await Task.FromResult(lateTime);
        }

        public async Task<ReservationData> GetReservationDetailsAsync()
        {
            // make request for reservation details
            return await Task.FromResult(_reservationData);
        }

        public void UpdateReservationDetails(ReservationData reservation)
        {
            // make request to update user's reservation details
            _reservationData = reservation;
        }
    }
}
