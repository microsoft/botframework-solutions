using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Models
{
    public class CalendarSkillState
    {
        public CalendarSkillState()
        {
            UserInfo = new UserInformation();
            APIToken = null;
            EventSource = EventSource.Other;
            CacheModel = null;
        }

        public UserInformation UserInfo { get; set; } = new UserInformation();

        public Luis.calendarLuis LuisResult { get; set; }

        public Luis.General GeneralLuisResult { get; set; }

        public string APIToken { get; set; }

        public EventSource EventSource { get; set; }

        public int PageSize { get; set; }

        public CalendarDialogStateBase CacheModel { get; set; }

        public TimeZoneInfo GetUserTimeZone()
        {
            if ((UserInfo != null) && (UserInfo.Timezone != null))
            {
                return UserInfo.Timezone;
            }

            return TimeZoneInfo.Local;
        }

        public void Clear()
        {
            APIToken = null;
            EventSource = EventSource.Other;
            CacheModel = null;
        }

        public class UserInformation
        {
            public string Name { get; set; }

            public TimeZoneInfo Timezone { get; set; }

            public double Latitude { get; set; }

            public double Longitude { get; set; }
        }
    }
}
