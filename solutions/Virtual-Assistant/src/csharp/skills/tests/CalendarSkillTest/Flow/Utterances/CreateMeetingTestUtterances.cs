using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class CreateMeetingTestUtterances : BaseTestUtterances
    {
        public CreateMeetingTestUtterances()
        {
            this.Add(BaseCreateMeeting, GetBaseCreateMeetingIntent());
        }

        public static string BaseCreateMeeting { get; } = "Create a meeting";

        private Calendar GetBaseCreateMeetingIntent()
        {
            var intent = new Calendar();
            intent.Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>();
            intent.Intents.Add(Calendar.Intent.CreateCalendarEntry, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new Calendar._Entities();
            return intent;
        }
    }
}
