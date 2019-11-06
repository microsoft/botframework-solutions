﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace AutomotiveSkill.Tests.Flow.Fakes
{
    public class MockVehicleSettingsIntent : SettingsLuis
    {
        private Intent intent;
        private double score;

        public MockVehicleSettingsIntent(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                throw new ArgumentNullException(nameof(userInput));
            }

            Entities = new _Entities();
            Intents = new Dictionary<Intent, IntentScore>();

            this.UserInput = userInput;

            (intent, score) = ProcessUserInput();
        }

        public string UserInput { get; set; }

        private (Intent intent, double score) ProcessUserInput()
        {
            var intentScore = new Microsoft.Bot.Builder.IntentScore
            {
                Score = 0.9909704,
                Properties = new Dictionary<string, object>()
            };

            switch (UserInput.ToLower())
            {
                case "set temperature to 21 degrees":
                    Entities.SETTING = new string[] { "temperature" };
                    Entities.AMOUNT = new string[] { "21" };
                    Entities.UNIT = new string[] { "degrees" };
                    break;
                case "increase temperature by 2":
                    Entities.VALUE = new string[] { "increase" };
                    Entities.SETTING = new string[] { "temperature" };
                    Entities.TYPE = new string[] { "by" };
                    Entities.AMOUNT = new string[] { "2" };
                    break;
                case "increase temperature to 24":
                    Entities.VALUE = new string[] { "increase" };
                    Entities.SETTING = new string[] { "temperature" };
                    Entities.AMOUNT = new string[] { "24" };
                    break;
                case "change the temperature":
                    Entities.SETTING = new string[] { "temperature" };
                    break;
                case "turn lane assist off":
                    Entities.SETTING = new string[] { "lane assist" };
                    Entities.VALUE = new string[] { "off" };
                    break;
                case "warm up the back of the car":
                    Entities.SETTING = new string[] { "back" };
                    Entities.VALUE = new string[] { "warm up" };
                    break;
                case "defog my windshield":
                    Entities.VALUE = new string[] { "defog" };
                    break;
                case "put the air on my feet":
                    Entities.SETTING = new string[] { "air" };
                    Entities.VALUE = new string[] { "feet" };
                    break;
                case "turn off the ac":
                    Entities.SETTING = new string[] { "ac" };
                    Entities.VALUE = new string[] { "off" };
                    break;
                case "increase forward automatic braking to 50%":
                    Entities.SETTING = new string[] { "forward automatic braking" };
                    Entities.VALUE = new string[] { "increase" };
                    Entities.AMOUNT = new string[] { "50" };
                    Entities.UNIT = new string[] { "%" };
                    break;
                case "i'm feeling cold":
                    Entities.VALUE = new string[] { "cold" };
                    Intents.Add(Intent.VEHICLE_SETTINGS_DECLARATIVE, intentScore);
                    break;
                case "it's feeling cold in the back":
                    Entities.SETTING = new string[] { "back" };
                    Entities.VALUE = new string[] { "cold" };
                    Intents.Add(Intent.VEHICLE_SETTINGS_DECLARATIVE, intentScore);
                    break;
                case "adjust equalizer":
                    Entities.SETTING = new string[] { "equalizer" };
                    break;
                case "change pedestrian detection":
                    Entities.SETTING = new string[] { "pedestrian detection" };
                    break;
                default:
                    return (Intent.None, 0.0);
            }

            // Default is setting change apart from declarative used occasionally above
            if (Intents.Count == 0)
            {
                Intents.Add(Intent.VEHICLE_SETTINGS_CHANGE, intentScore);
            }

            return TopIntent();
        }
    }
}