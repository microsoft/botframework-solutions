// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;

namespace DemoSkill
{
    public class DemoSkillServices
    {
        public string AuthConnectionName { get; set; }

        public CosmosDbStorageOptions CosmosDbOptions { get; set; }
    }
}
