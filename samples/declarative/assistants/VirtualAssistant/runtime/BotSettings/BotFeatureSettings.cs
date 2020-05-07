// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.ComposerBot.Json
{
    public class BotFeatureSettings
    {
        // Use TranscriptLoggerMiddleware 
        public bool UseTranscriptLoggerMiddleware { get; set; }

        // Use ShowTypingMiddleware
        public bool UseShowTypingMiddleware { get; set; }

        // Use InspectionMiddleware
        public bool UseInspectionMiddleware { get; set; }

        // Use CosmosDb for storage
        public bool UseCosmosDbPersistentStorage { get; set; }
    }
}
