using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace PointOfInterestSkillTests.Flow.Fakes
{
    public class MockPointOfInterestIntent : PointOfInterestLuis
    {
        private Intent intent;
        private double score;

        public MockPointOfInterestIntent(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                throw new ArgumentNullException(nameof(userInput));
            }

            this.Entities = new _Entities();
            this.Intents = new Dictionary<Intent, IntentScore>();

            this.UserInput = userInput;

            (intent, score) = ProcessUserInput();
        }

        public string UserInput { get; set; }

        private (Intent intent, double score) ProcessUserInput()
        {
            var intentScore = new Microsoft.Bot.Builder.IntentScore();
            intentScore.Score = 0.9909704;
            intentScore.Properties = new Dictionary<string, object>();

            switch (UserInput.ToLower())
            {
                case "what's nearby?":
                    this.Intents.Add(Intent.NAVIGATION_FIND_POINTOFINTEREST, intentScore);
                    break;
                case "cancel my route":
                    this.Intents.Add(Intent.NAVIGATION_CANCEL_ROUTE, intentScore);
                    break;
                case "find a route":
                    this.Intents.Add(Intent.NAVIGATION_ROUTE_FROM_X_TO_Y, intentScore);
                    break;
                case "get directions to microsoft corporation":
                    this.Entities.KEYWORD = new string[] { "microsoft corporation" };
                    this.Intents.Add(Intent.NAVIGATION_ROUTE_FROM_X_TO_Y, intentScore);
                    break;
                case "get directions to the pharmacy":
                    this.Entities.KEYWORD = new string[] { "pharmacy" };
                    this.Intents.Add(Intent.NAVIGATION_ROUTE_FROM_X_TO_Y, intentScore);
                    break;
                case "find a parking garage":
                    this.Intents.Add(Intent.NAVIGATION_FIND_PARKING, intentScore);
                    break;
                case "find a parking garage near 1635 11th ave":
                    this.Entities.KEYWORD = new string[] { "1635 11th ave" };
                    this.Intents.Add(Intent.NAVIGATION_FIND_PARKING, intentScore);
                    break;
                case "option 1":
                    this.Intents.Add(Intent.NAVIGATION_ROUTE_FROM_X_TO_Y, intentScore);
                    this.Entities.number = new double[] { 1 };
                    break;
                default:
                    return (Intent.None, 0.0);
            }

            return this.TopIntent();
        }
    }
}