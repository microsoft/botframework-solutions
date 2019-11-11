// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        Task<TimeSpan> GetLateCheckOutAsync();

        // request items to be brought
        Task<bool> RequestItems(List<ItemRequestClass> items);

        // check item request availability
        RoomItem CheckRoomItemAvailability(string item);

        // check availability of a room service request
        MenuItem CheckMenuItemAvailability(string item);

        // get the requested menu to view
        Menu GetMenu(string menuType);
    }
}
