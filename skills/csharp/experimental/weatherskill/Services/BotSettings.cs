// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions;

namespace WeatherSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string WeatherApiKey { get; set; }

        public string BingSpellCheckSubscriptionKey { get; set; }
    }
}