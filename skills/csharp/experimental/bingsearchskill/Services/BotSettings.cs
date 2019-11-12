// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions;

namespace BingSearchSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string BingSearchKey { get; set; }

        public string BingAnswerSearchKey { get; set; }

        public string BingSpellCheckSubscriptionKey { get; set; }
    }
}