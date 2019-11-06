﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace AutomotiveSkill.Tests.Flow.Fakes
{
    public class MockVehicleSettingsValueIntent : SettingsValueLuis
    {
        private Intent intent;
        private double score;

        public MockVehicleSettingsValueIntent(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                throw new ArgumentNullException(nameof(userInput));
            }

            this.Entities = new SettingsValueLuis._Entities();
            this.Intents = new Dictionary<Intent, IntentScore>();

            var intentScore = new Microsoft.Bot.Builder.IntentScore();
            intentScore.Score = 0.9909704;
            intentScore.Properties = new Dictionary<string, object>();

            this.Intents.Add(SettingsValueLuis.Intent.SETTING_VALUE_SELECTION, intentScore);

            switch (userInput.ToLower())
            {
                case "second one":
                    this.Entities.INDEX = new string[] { "second" };
                    break;
                case "braking and alerts":
                case "decrease":
                case "increase":
                case "steer":
                    this.Entities.VALUE = new string[] { userInput.ToLower() };
                    break;
            }

            (intent, score) = this.TopIntent();
        }
    }
}