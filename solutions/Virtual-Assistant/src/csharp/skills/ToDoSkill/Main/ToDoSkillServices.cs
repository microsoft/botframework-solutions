// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using Microsoft.ApplicationInsights;
    using Microsoft.Bot.Builder.AI.Luis;
    using Microsoft.Bot.Builder.Azure;

    public class ToDoSkillServices
    {
        public string AuthConnectionName { get; set; }

        public CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public LuisRecognizer LuisRecognizer { get; set; }

        public TelemetryClient TelemetryClient { get; set; }
    }
}
