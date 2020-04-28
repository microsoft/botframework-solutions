// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;

namespace Microsoft.Bot.Solutions
{
    public class AdaptiveCognitiveModelSet
    {
        public LuisAdaptiveRecognizer DispatchService { get; set; }

        public Dictionary<string, LuisAdaptiveRecognizer> LuisServices { get; set; } = new Dictionary<string, LuisAdaptiveRecognizer>();

        public Dictionary<string, QnAMakerEndpoint> QnAConfiguration { get; set; } = new Dictionary<string, QnAMakerEndpoint>();
    }
}