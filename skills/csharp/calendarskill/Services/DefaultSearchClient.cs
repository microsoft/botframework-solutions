using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;

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
