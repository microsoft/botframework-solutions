using AutomotiveSkill;
using Luis;
using Microsoft.Bot.Builder;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AutomotiveSkillTest.Flow.Fakes
{
    public class MockVehicleSettingsNameIntent : VehicleSettingsNameSelection
    {
        public string userInput;
        private Intent intent;
        private double score;     

        public MockVehicleSettingsNameIntent(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                throw new ArgumentNullException(nameof(userInput));
            }

            this.Entities = new VehicleSettingsNameSelection._Entities();
            this.Intents = new Dictionary<Intent, IntentScore>();

            this.userInput = userInput;

            var intentScore = new Microsoft.Bot.Builder.IntentScore();
            intentScore.Score = 0.9909704;
            intentScore.Properties = new Dictionary<string, object>();

            // This LUIS model is for followup setting clarification so we assume input for test purposes is the setting name
            this.Intents.Add(VehicleSettingsNameSelection.Intent.SETTING_NAME_SELECTION, intentScore);
            this.Entities.SETTING = new string[] { userInput.ToLower() };

            (intent, score) = this.TopIntent();
        }            
    }
}