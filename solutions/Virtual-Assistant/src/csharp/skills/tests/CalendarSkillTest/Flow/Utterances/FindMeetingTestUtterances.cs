using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class FindMeetingTestUtterances : BaseTestUtterances
    {
        public FindMeetingTestUtterances()
        {
            this.Add(BaseFindMeeting, GetBaseFindMeetingIntent());
            this.Add(BaseNextMeeting, GetBaseNextMeetingIntent());
        }

        public static string BaseFindMeeting { get; } = "What should I do today";

        public static string BaseNextMeeting { get; } = "what is my next meeting";

        private Calendar GetBaseFindMeetingIntent()
        {
            var intent = new Calendar();
            intent.Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>();
            intent.Intents.Add(Calendar.Intent.FindCalendarEntry, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new Calendar._Entities();
            return intent;
        }

        private Calendar GetBaseNextMeetingIntent()
        {
            var intent = new Calendar();
            intent.Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>();
            intent.Intents.Add(Calendar.Intent.NextMeeting, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new Calendar._Entities();
            return intent;
        }
    }
}
