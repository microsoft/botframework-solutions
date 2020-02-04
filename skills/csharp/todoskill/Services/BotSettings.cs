// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions;

namespace ToDoSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public int DisplaySize { get; set; }

        public string TaskServiceProvider { get; set; }
    }
}