// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Builder.Solutions
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class CognitiveModelSet
    {
        public IRecognizer DispatchService { get; set; }

        public Dictionary<string, LuisRecognizer> LuisServices { get; set; } = new Dictionary<string, LuisRecognizer>();

        public Dictionary<string, QnAMaker> QnAServices { get; set; } = new Dictionary<string, QnAMaker>();
    }
}