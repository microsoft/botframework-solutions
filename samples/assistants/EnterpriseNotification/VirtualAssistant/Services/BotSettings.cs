// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;

namespace VirtualAssistant.Services
{
    public class BotSettings : BotSettingsBase
    {
        public List<SkillManifest> Skills { get; set; } = new List<SkillManifest>();

        /// <summary>
        /// Gets or sets the CosmosDB Configuration for maintaining the conversation reference objects
        /// for proactively sending messages to users
        /// </summary>
        /// <value>
        /// The CosmosDB Configuration for the bot.
        /// </value>
        public CosmosDbStorageOptions CosmosDbProactive { get; set; }
    }
}