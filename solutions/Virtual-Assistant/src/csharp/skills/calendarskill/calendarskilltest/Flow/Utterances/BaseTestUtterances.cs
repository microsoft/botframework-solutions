using System;
using System.Collections.Generic;
using System.Text;
using CalendarSkillTest.Flow.Fakes;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace CalendarSkillTest.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, Calendar>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public Calendar GetBaseNoneIntent()
        {
            return GetCalendarIntent();
        }

        protected Calendar GetCalendarIntent(
            string userInput = null,
            Calendar.Intent intents = Calendar.Intent.None,
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
            string[] orderReference = null)
        {
            var intent = new Calendar
            {
                Text = userInput,
                Intents = new Dictionary<Calendar.Intent, IntentScore>()
            };
            intent.Intents.Add(intents, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new Calendar._Entities
            {
                _instance = new Calendar._Entities._Instance(),
                ordinal = ordinal,
                number = number,
                Subject = subject,
                ContactName = contactName
            };
            intent.Entities._instance.ContactName = GetInstanceDatas(userInput, contactName);
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

        private InstanceData[] GetInstanceDatas(string userInput, string[] entities)
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
