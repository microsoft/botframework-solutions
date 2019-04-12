using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace CalendarSkillTest.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, CalendarLuis>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public CalendarLuis GetBaseNoneIntent()
        {
            return GetCalendarIntent();
        }

        protected static CalendarLuis GetCalendarIntent(
            string userInput = null,
            CalendarLuis.Intent intents = CalendarLuis.Intent.None,
            double[] ordinal = null,
            double[] number = null,
            string[] subject = null,
            string[] contactName = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] duration = null,
            string[] meetingRoom = null,
            string[] location = null,
            string[] moveEarlierTimeSpan = null,
            string[] moveLaterTimeSpan = null,
            string[] orderReference = null,
            string[] askParameter = null)
        {
            var intent = new CalendarLuis
            {
                Text = userInput,
                Intents = new Dictionary<CalendarLuis.Intent, IntentScore>()
            };
            intent.Intents.Add(intents, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new CalendarLuis._Entities
            {
                _instance = new CalendarLuis._Entities._Instance(),
                ordinal = ordinal,
                number = number,
                Subject = subject,
                personName = contactName
            };
            intent.Entities._instance.personName = GetInstanceDatas(userInput, contactName);
            intent.Entities.FromDate = fromDate;
            intent.Entities._instance.FromDate = GetInstanceDatas(userInput, fromDate);
            intent.Entities.ToDate = toDate;
            intent.Entities._instance.ToDate = GetInstanceDatas(userInput, toDate);
            intent.Entities.FromTime = fromTime;
            intent.Entities._instance.FromTime = GetInstanceDatas(userInput, fromTime);
            intent.Entities.ToTime = toTime;
            intent.Entities._instance.ToTime = GetInstanceDatas(userInput, toTime);
            intent.Entities.Duration = duration;
            intent.Entities.MeetingRoom = meetingRoom;
            intent.Entities.Location = location;
            intent.Entities.MoveEarlierTimeSpan = moveEarlierTimeSpan;
            intent.Entities.MoveLaterTimeSpan = moveLaterTimeSpan;
            intent.Entities.OrderReference = orderReference;
            intent.Entities._instance.OrderReference = GetInstanceDatas(userInput, orderReference);
            return intent;
        }

        private static InstanceData[] GetInstanceDatas(string userInput, string[] entities)
        {
            if (userInput == null || entities == null)
            {
                return null;
            }

            var result = new InstanceData[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                var name = entities[i];
                var index = userInput.IndexOf(name);
                if (index == -1)
                {
                    throw new Exception("No such string in user input");
                }

                var instanceData = new InstanceData
                {
                    StartIndex = index,
                    EndIndex = index + name.Length,
                    Text = name
                };

                result[i] = instanceData;
            }

            return result;
        }
    }
}
