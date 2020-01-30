// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions;

namespace NewsSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string BingNewsKey { get; set; }

        public string AzureMapsKey { get; set; }
    }
}