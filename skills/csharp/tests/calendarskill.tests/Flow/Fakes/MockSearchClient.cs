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
        private static List<RoomModel> meetingRooms = null;

        public MockSearchClient()
        {
            SetAllToDefault();
        }

        public static void SetAllToDefault()
        {
            meetingRooms = new List<RoomModel>();
            var numberOfBuilding = 2;
            var numberOfFloorOnEachBuilding = 2;
            var numberOfRoomOnEachFloor = 2;

            for (int i = 0; i < numberOfBuilding; i++)
            {
                for (int j = 0; j < numberOfFloorOnEachBuilding; j++)
                {
                    for (int k = 0; k < numberOfRoomOnEachFloor; k++)
                    {
                        var buildingNumber = i + 1;
                        var floorNumber = j + 1;
                        var roomNumber = (i * numberOfFloorOnEachBuilding * numberOfRoomOnEachFloor) + (j * numberOfRoomOnEachFloor) + k + 1;
                        meetingRooms.Add(new RoomModel()
                        {
                            Id = string.Format(Strings.Strings.MeetingRoomId, roomNumber),
                            DisplayName = string.Format(Strings.Strings.MeetingRoomName, roomNumber),
                            EmailAddress = string.Format(Strings.Strings.MeetingRoomEmail, roomNumber),
                            Building = string.Format(Strings.Strings.Building, buildingNumber),
                            FloorNumber = floorNumber,
                        });
                    }
                }
            }
        }

        public static void SetNonMeetingRoom()
        {
            meetingRooms = new List<RoomModel>();
        }

        public static void SetSingleMeetingRoom()
        {
            meetingRooms = new List<RoomModel>();
            var floorNumber = 1;
            meetingRooms.Add(new RoomModel()
            {
                Id = Strings.Strings.DefaultMeetingRoomId,
                DisplayName = Strings.Strings.DefaultMeetingRoomName,
                EmailAddress = Strings.Strings.DefaultUserEmail,
                Building = Strings.Strings.DefaultBuilding,
                FloorNumber = floorNumber,
            });
        }

        public static void SetSingleFloorMultiMeetingRoom()
        {
            meetingRooms = new List<RoomModel>();
            var buildingNumber = 1;
            var floorNumber = 1;
            for (int i = 0; i < 2; i++)
            {
                var roomNumber = i + 1;
                meetingRooms.Add(new RoomModel()
                {
                    Id = string.Format(Strings.Strings.MeetingRoomId, roomNumber),
                    DisplayName = string.Format(Strings.Strings.MeetingRoomName, roomNumber),
                    EmailAddress = Strings.Strings.DefaultUserEmail,
                    Building = string.Format(Strings.Strings.Building, buildingNumber),
                    FloorNumber = floorNumber,
                });
            }
        }

        public static void SetSingleBuildingSingleFloorMultiMeetingRoom()
        {
            meetingRooms = new List<RoomModel>();
            var buildingNumber = 1;
            var floorNumber = 1;
            var roomNumber = 1;
            meetingRooms.Add(new RoomModel()
            {
                Id = string.Format(Strings.Strings.MeetingRoomId, roomNumber),
                DisplayName = string.Format(Strings.Strings.MeetingRoomName, roomNumber),
                EmailAddress = Strings.Strings.DefaultUserEmail,
                Building = string.Format(Strings.Strings.Building, buildingNumber),
                FloorNumber = floorNumber,
            });
        }

        public async Task<List<RoomModel>> GetMeetingRoomAsync(string query, int floorNumber = 0)
        {
            return meetingRooms.FindAll(room => (room.Building == query || room.DisplayName == query) && (floorNumber == 0 || room.FloorNumber == floorNumber));
        }
    }
}
