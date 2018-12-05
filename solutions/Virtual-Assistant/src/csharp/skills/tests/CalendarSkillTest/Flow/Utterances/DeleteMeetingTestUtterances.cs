using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class DeleteMeetingTestUtterances : BaseTestUtterances
    {
        public DeleteMeetingTestUtterances()
        {
            this.Add(BaseDeleteMeeting, GetBaseDeleteMeetingIntent());
        }

        public static string BaseDeleteMeeting { get; } = "delete meeting";

        private Calendar GetBaseDeleteMeetingIntent()
        {
            var intent = new Calendar();
            intent.Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>();
            intent.Intents.Add(Calendar.Intent.DeleteCalendarEntry, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new Calendar._Entities();
            return intent;
        }
    }
}
