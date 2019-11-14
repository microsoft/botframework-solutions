// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using Newtonsoft.Json;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Services
{
    // Mock hotel service
    // Should replace with real apis
    public class HotelService : IHotelService
    {
        public static readonly int StayDays = 4;
        public static readonly TimeSpan CheckOutTime = new TimeSpan(12, 0, 0);
        public static readonly TimeSpan LateTime = new TimeSpan(16, 0, 0);

        private const string RoomServiceMenuFileName = "RoomServiceMenu.json";
        private const string AvailableItemsFileName = "AvailableItems.json";

        private readonly string _menuFilePath;
        private readonly string _availableItemsFilePath;
        private readonly DateTime? _checkInDate;

        public HotelService(DateTime? checkInDate = null)
        {
            _menuFilePath = typeof(HotelService).Assembly
                .GetManifestResourceNames()
                .Where(x => x.Contains(RoomServiceMenuFileName))
                .First();

            _availableItemsFilePath = typeof(HotelService).Assembly
                .GetManifestResourceNames()
                .Where(x => x.Contains(AvailableItemsFileName))
                .First();

            _checkInDate = checkInDate;
        }

        public async Task<TimeSpan> GetLateCheckOutAsync()
        {
            // make request for the late check out time
            return await Task.FromResult(LateTime);
        }

        public Task<ReservationData> GetReservationDetails()
        {
            // make request for reservation details
            var date = _checkInDate ?? DateTime.Now;
            return Task.FromResult(new ReservationData
            {
                CheckInDate = date.ToString(ReservationData.DateFormat),
                CheckOutDate = date.AddDays(StayDays).ToString(ReservationData.DateFormat),
                CheckOutTimeData = CheckOutTime
            });
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

        public RoomItem CheckRoomItemAvailability(string item)
        {
            using (var r = new StreamReader(typeof(HotelService).Assembly.GetManifestResourceStream(_availableItemsFilePath)))
            {
                string json = r.ReadToEnd();
                RoomItem[] roomItems = JsonConvert.DeserializeObject<RoomItem[]>(json);

                // check all item names
                foreach (var roomItem in roomItems)
                {
                    if (Array.Exists(roomItem.Names, x => string.Equals(x, item, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        return roomItem;
                    }
                }

                return null;
            }
        }

        // returns full name of menu item if found
        public MenuItem CheckMenuItemAvailability(string item)
        {
            using (var r = new StreamReader(typeof(HotelService).Assembly.GetManifestResourceStream(_menuFilePath)))
            {
                string json = r.ReadToEnd();
                Menu[] menus = JsonConvert.DeserializeObject<Menu[]>(json);

                // check all menus
                foreach (var menu in menus)
                {
                    foreach (var menuItem in menu.Items)
                    {
                        if (Array.Exists(menuItem.AllNames, x => string.Equals(x, item, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            return menuItem;
                        }
                    }
                }

                return null;
            }
        }

        // gets requested menu details
        public Menu GetMenu(string menuType)
        {
            using (var r = new StreamReader(typeof(HotelService).Assembly.GetManifestResourceStream(_menuFilePath)))
            {
                string json = r.ReadToEnd();
                Menu[] menus = JsonConvert.DeserializeObject<Menu[]>(json);
                return Array.Find(menus, x => string.Equals(x.Type, menuType, StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }
}
