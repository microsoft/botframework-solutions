// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;

namespace PointOfInterestSkill
{
    public class PointOfInterestSkillServices
    {
        public string AuthConnectionName { get; set; }

        public string AzureMapsKey { get; set; }

        public CosmosDbStorageOptions CosmosDbOptions { get; set; }

        public LuisRecognizer LuisRecognizer { get; set; }

        public TelemetryClient TelemetryClient { get; set; }
    }
}
