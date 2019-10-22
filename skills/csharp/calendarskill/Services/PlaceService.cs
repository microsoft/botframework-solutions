using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;

namespace CalendarSkill.Services
{
    public class PlaceService : IPlaceService
    {
        private IPlaceService placeAPI;

        public PlaceService()
        {
            // to get pass when serialize
        }

        public PlaceService(IPlaceService placeAPI, EventSource source)
        {
            this.placeAPI = placeAPI ?? throw new Exception("calendarAPI is null");
        }

        /// <inheritdoc/>
        public async Task<List<PlaceModel>> GetMeetingRoomByTitleAsync(string title)
        {
            return await placeAPI.GetMeetingRoomByTitleAsync(title);
        }

        public async Task<List<PlaceModel>> GetMeetingRoomAsync()
        {
            return await placeAPI.GetMeetingRoomAsync();
        }
    }
}
