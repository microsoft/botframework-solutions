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
        private const string RoomServiceMenuFileName = "RoomServiceMenu.json";
        private readonly string _menuFilePath;

        public HotelService()
        {
            _menuFilePath = typeof(HotelService).Assembly
                .GetManifestResourceNames()
                .Where(x => x.Contains(RoomServiceMenuFileName))
                .First();
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
