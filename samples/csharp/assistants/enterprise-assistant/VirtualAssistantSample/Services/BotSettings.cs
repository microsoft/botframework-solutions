﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;

namespace VirtualAssistantSample.Services
{
    public class BotSettings : BotSettingsBase
    {
        public List<SkillManifest> Skills { get; set; } = new List<SkillManifest>();
    }
}