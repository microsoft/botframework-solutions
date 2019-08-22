// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions;

namespace MusicSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string SpotifyClientId { get; set; }

        public string SpotifyClientSecret { get; set; }
    }
}