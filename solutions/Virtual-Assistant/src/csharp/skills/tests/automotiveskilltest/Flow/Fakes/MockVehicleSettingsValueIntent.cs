using AutomotiveSkill;
using Luis;
using Microsoft.Bot.Builder;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AutomotiveSkillTest.Flow.Fakes
{
    public class MockVehicleSettingsValueIntent : VehicleSettingsValueSelection
    {
        public string userInput;
        private Intent intent;
        private double score;     

        public MockVehicleSettingsValueIntent(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                throw new ArgumentNullException(nameof(userInput));
            }

            this.Entities = new VehicleSettingsValueSelection._Entities();
            this.Intents = new Dictionary<Intent, IntentScore>();

            var intentScore = new Microsoft.Bot.Builder.IntentScore();
            intentScore.Score = 0.9909704;
            intentScore.Properties = new Dictionary<string, object>();

            // This LUIS model is for followup setting clarification so we assume input for test purposes is the setting name
            this.Intents.Add(VehicleSettingsValueSelection.Intent.SETTING_VALUE_SELECTION, intentScore);
            this.Entities.VALUE = new string[] { userInput.ToLower() };

            (intent, score) = this.TopIntent();
        }            
    }
}