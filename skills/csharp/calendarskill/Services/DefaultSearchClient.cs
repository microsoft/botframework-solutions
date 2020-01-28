using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Rest.Azure;

namespace CalendarSkill.Services.AzureSearchAPI
{
    public class DefaultSearchClient : ISearchService
    {

        public DefaultSearchClient()
        {
        }

        public async Task<List<RoomModel>> GetMeetingRoomAsync(string query, int floorNumber = 0)
        {
            List<RoomModel> meetingRooms = new List<RoomModel>();

            return meetingRooms;
        }
    }
}
