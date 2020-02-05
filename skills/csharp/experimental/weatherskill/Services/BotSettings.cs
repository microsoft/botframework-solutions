// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Solutions;

namespace WeatherSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string WeatherApiKey { get; set; }
    }
}