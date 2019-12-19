using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;
using Microsoft.Graph;

namespace CalendarSkill.Test.Flow.Fakes
{
    public class MockSearchClient : ISearchService
    {

        private static List<RoomModel> MeetingRooms = null;
        public MockSearchClient()
        {
            MeetingRooms = new List<RoomModel>();
            MeetingRooms.Add(new RoomModel()
            {
                Id = "1",
                DisplayName = Strings.Strings.DefaultMeetingRoom,
                EmailAddress = Strings.Strings.DefaultUserEmail,
                Building = Strings.Strings.DefaultBuilding,
                FloorNumber = 1,
            });

            MeetingRooms.Add(new RoomModel()
            {
                Id = "2",
                DisplayName = Strings.Strings.DefaultMeetingRoom2,
                EmailAddress = Strings.Strings.DefaultUserEmail,
                Building = Strings.Strings.DefaultBuilding,
                FloorNumber = 1,
            });

            MeetingRooms.Add(new RoomModel()
            {
                Id = "3",
                DisplayName = Strings.Strings.DefaultMeetingRoom3,
                EmailAddress = Strings.Strings.DefaultUserEmail,
                Building = Strings.Strings.DefaultBuilding,
                FloorNumber = 2,
            });
        }

        public static void SetSingleMeetingRoom()
        {
            MeetingRooms = new List<RoomModel>();
            MeetingRooms.Add(new RoomModel()
            {
                Id = "1",
                DisplayName = Strings.Strings.DefaultMeetingRoom,
                EmailAddress = Strings.Strings.DefaultUserEmail,
                Building = Strings.Strings.DefaultBuilding,
                FloorNumber = 1,
            });
        }

        public async Task<List<RoomModel>> GetMeetingRoomAsync(string query, int floorNumber = 0)
        {
            List<RoomModel> meetingRooms = MeetingRooms;
            return meetingRooms;
        }
    }
}
