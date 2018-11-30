using AutomotiveSkill;
using Luis;
using Microsoft.Bot.Builder;
using System.Collections;
using System.Collections.Generic;

namespace AutomotiveSkillTest.Flow.Fakes
{
    public class MockVehicleSettingsIntent : VehicleSettings
    {
        private string userInput;
        private Intent intent;
        private double score;     

        public MockVehicleSettingsIntent(string userInput)
        {
            this.Entities = new VehicleSettings._Entities();
            this.Intents = new Dictionary<Intent, IntentScore>();

            this.userInput = userInput;

            (intent, score) = ForwardEmailTestLuisResultMock();

            //if (intent == VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE)
            //{
            //    (intent, score) = ForwardEmailTestLuisResultMock();
            //}           
            //else
            //{
            //    this.intent = VehicleSettings.Intent.None;
            //    this.score = 0;                
            //}
        }

        public override _Entities Entities { get; set; }

        public override (Intent intent, double score) TopIntent()
        {
            return (intent, score);
        }

        private (Intent intent, double score) ForwardEmailTestLuisResultMock()
        {
            if (userInput.ToLower() == "set temperature to 21 degrees")
            {
                var intentScore = new Microsoft.Bot.Builder.IntentScore();
                intentScore.Score = 0.9909704;
                intentScore.Properties = new Dictionary<string, object>();

                this.Intents.Add(VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE, intentScore);

                this.Entities.SETTING = new string[] { "temperature" };
                this.Entities.AMOUNT = new string[] { "21" };
                this.Entities.UNIT = new string[] { "degrees" };

                return (VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE, 0.9909704);
            }
            else
            {
                return (VehicleSettings.Intent.None, 0.0);
            }
        }      
    }
}