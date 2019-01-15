// <auto-generated>
// Code generated by LUISGen
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace Luis
{
    public class Calendar: IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent {
            ChangeCalendarEntry, 
            CheckAvailability, 
            ConnectToMeeting, 
            ContactMeetingAttendees, 
            CreateCalendarEntry, 
            DeleteCalendarEntry, 
            FindCalendarDetail, 
            FindCalendarEntry, 
            FindCalendarWhen, 
            FindCalendarWhere, 
            FindCalendarWho, 
            FindDuration, 
            FindMeetingRoom, 
            GoBack, 
            NextMeeting, 
            NoLocation, 
            None, 
            ReadAloud, 
            Summary, 
            TimeRemaining
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] Duration;
            public string[] Subject;
            public string[] ContactName;
            public string[] MoveEarlierTimeSpan;
            public string[] MoveLaterTimeSpan;
            public string[] SlotAttribute;
            public string[] Location;
            public string[] OrderReference;
            public string[] PositionReference;
            public string[] RelationshipName;
            public string[] MeetingRoom;
            public string[] DestinationCalendar;
            public string[] FromDate;
            public string[] FromTime;
            public string[] ToDate;
            public string[] ToTime;
            public string[] AskParameter;

            // Built-in entities
            public DateTimeSpec[] datetime;
            public double[] number;
            public double[] ordinal;

            // Instance
            public class _Instance
            {
                public InstanceData[] Duration;
                public InstanceData[] Subject;
                public InstanceData[] ContactName;
                public InstanceData[] MoveEarlierTimeSpan;
                public InstanceData[] MoveLaterTimeSpan;
                public InstanceData[] SlotAttribute;
                public InstanceData[] Location;
                public InstanceData[] OrderReference;
                public InstanceData[] PositionReference;
                public InstanceData[] RelationshipName;
                public InstanceData[] MeetingRoom;
                public InstanceData[] DestinationCalendar;
                public InstanceData[] datetime;
                public InstanceData[] number;
                public InstanceData[] ordinal;
                public InstanceData[] FromDate;
                public InstanceData[] FromTime;
                public InstanceData[] ToDate;
                public InstanceData[] ToTime;
                public InstanceData[] AskParameter;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<Calendar>(JsonConvert.SerializeObject(result));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
