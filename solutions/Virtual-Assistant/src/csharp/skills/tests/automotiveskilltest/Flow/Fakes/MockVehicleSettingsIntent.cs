using AutomotiveSkill;
using Luis;
using Microsoft.Bot.Builder;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AutomotiveSkillTest.Flow.Fakes
{
    public class MockVehicleSettingsIntent : VehicleSettings
    {
        public string userInput;
        private Intent intent;
        private double score;     

        public MockVehicleSettingsIntent(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                throw new ArgumentNullException(nameof(userInput));
            }

            this.Entities = new VehicleSettings._Entities();
            this.Intents = new Dictionary<Intent, IntentScore>();

            this.userInput = userInput;

            (intent, score) = ProcessUserInput();
        }
        
        private (Intent intent, double score) ProcessUserInput()
        {           
            switch (userInput.ToLower())
            {
                case "set temperature to 21 degrees":                                
                    this.Entities.SETTING = new string[] { "temperature" };
                    this.Entities.AMOUNT = new string[] { "21" };
                    this.Entities.UNIT = new string[] { "degrees" };
                    break;
                case "turn lane assist off":                                   
                    this.Entities.SETTING = new string[] { "lane assist" };
                    this.Entities.VALUE = new string[] { "off" };
                    break;
                case "warm up the back of the car":
                    this.Entities.SETTING = new string[] { "back" };
                    this.Entities.VALUE = new string[] { "warm up" };
                    break;
                case "defog my windshield":
                    this.Entities.VALUE = new string[] { "defog" };
                    break;
                case "put the air on my feet":            
                    this.Entities.SETTING = new string[] { "air" };
                    this.Entities.VALUE = new string[] { "feet" };
                    break;
                case "turn off the ac":
                    this.Entities.SETTING = new string[] { "ac" };
                    this.Entities.VALUE = new string[] { "off" };
                    break;
                case "increase forward automatic braking to 50%":          
                    this.Entities.SETTING = new string[] { "forward automatic braking" };
                    this.Entities.VALUE = new string[] { "increase" };
                    this.Entities.AMOUNT = new string[] { "50" };
                    this.Entities.UNIT = new string[] { "%" };
                    break;
                default:
                    return (VehicleSettings.Intent.None, 0.0);
            }

            var intentScore = new Microsoft.Bot.Builder.IntentScore();
            intentScore.Score = 0.9909704;
            intentScore.Properties = new Dictionary<string, object>();
            this.Intents.Add(VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE, intentScore);

            return (VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE, intentScore.Score.Value);
        }
    }
}