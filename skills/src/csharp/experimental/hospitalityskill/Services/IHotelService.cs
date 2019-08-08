﻿using System.Collections.Generic;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Services
{
    // Service used to call hotel apis
    public interface IHotelService
    {
        // get reservation details
        Task<ReservationData> GetReservationDetails();

        // update reservation
        void UpdateReservationDetails(ReservationData reservation);

        // check late check out availability
        Task<string> GetLateCheckOutAsync();

        // request items to be brought
        Task<bool> RequestItems(List<ItemRequestClass> items);
    }
}
