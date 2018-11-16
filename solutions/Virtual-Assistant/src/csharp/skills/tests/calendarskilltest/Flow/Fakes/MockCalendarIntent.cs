using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarIntent : Calendar
    {
        private string userInput;
        private Intent intent;
        private double score;

        public MockCalendarIntent(string userInput)
        {
            this.Entities = new Calendar._Entities();
            this.Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>();
            this.userInput = userInput;

            this.intent = Calendar.Intent.None;
            this.score = 0;

            (intent, score) = SummaryCalendarTestLuisResultMock();

            if (intent == Calendar.Intent.None)
            {
                (intent, score) = CreateCalendarLuisResultMock();
            }

            if (intent == Calendar.Intent.None)
            {
                (intent, score) = DeleteCalendarLuisResultMock();
            }

            if (intent == Calendar.Intent.None)
            {
                (intent, score) = NextCalendarLuisResultMock();
            }

            if (intent == Calendar.Intent.None)
            {
                (intent, score) = UpdateCalendarLuisResultMock();
            }
        }

        public new _Entities Entities { get; set; }

        public new (Intent intent, double score) TopIntent()
        {
            return (intent, score);
        }

        private (Intent intent, double score) NextCalendarLuisResultMock()
        {
            if (userInput == "what is my next meeting")
            {
                IntentScore intentScore = new IntentScore();
                intentScore.Score = 0.9;
                this.Intents.Add(Calendar.Intent.NextMeeting, intentScore);
                return (Calendar.Intent.NextMeeting, 0.90);
            }

            return (Calendar.Intent.None, 0.0);
        }

        private (Intent intent, double score) UpdateCalendarLuisResultMock()
        {
            if (userInput == "update meeting")
            {
                IntentScore intentScore = new IntentScore();
                intentScore.Score = 0.9;
                this.Intents.Add(Calendar.Intent.ChangeCalendarEntry, intentScore);
                return (Calendar.Intent.ChangeCalendarEntry, 0.90);
            }

            return (Calendar.Intent.None, 0.0);
        }

        private (Intent intent, double score) CreateCalendarLuisResultMock()
        {
            if (userInput == "Create a meeting")
            {
                IntentScore intentScore = new IntentScore();
                intentScore.Score = 0.9;
                this.Intents.Add(Calendar.Intent.CreateCalendarEntry, intentScore);
                return (Calendar.Intent.CreateCalendarEntry, 0.90);
            }
            else if (userInput == "Yes")
            {
                return (Calendar.Intent.None, 0.90);
            }

            return (Calendar.Intent.None, 0.0);
        }

        private (Intent intent, double score) SummaryCalendarTestLuisResultMock()
        {
            if (userInput == "What should I do today")
            {
                IntentScore intentScore = new IntentScore();
                intentScore.Score = 0.9;
                this.Intents.Add(Calendar.Intent.Summary, intentScore);
                return (Calendar.Intent.Summary, 0.90);
            }
            else if (userInput == "No")
            {
                return (Calendar.Intent.None, 0.90);
            }

            return (Calendar.Intent.None, 0.0);
        }

        private (Intent intent, double score) DeleteCalendarLuisResultMock()
        {
            if (userInput == "delete meeting")
            {
                IntentScore intentScore = new IntentScore();
                intentScore.Score = 0.9;
                this.Intents.Add(Calendar.Intent.DeleteCalendarEntry, intentScore);
                return (Calendar.Intent.DeleteCalendarEntry, 0.90);
            }
            else if (userInput == "Yes")
            {
                return (Calendar.Intent.None, 0.90);
            }

            return (Calendar.Intent.None, 0.0);
        }
    }
}