using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class UpdateMeetingTestUtterances : BaseTestUtterances
    {
        public UpdateMeetingTestUtterances()
        {
            this.Add(BaseUpdateMeeting, GetBaseUpdateMeetingIntent());
        }

        public static string BaseUpdateMeeting { get; } = "update meeting";

        private Calendar GetBaseUpdateMeetingIntent()
        {
            var intent = new Calendar();
            intent.Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>();
            intent.Intents.Add(Calendar.Intent.ChangeCalendarEntry, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new Calendar._Entities();
            return intent;
        }
    }
}
