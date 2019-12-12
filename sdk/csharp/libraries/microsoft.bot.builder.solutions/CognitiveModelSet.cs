// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Builder.Solutions
{
    public class CognitiveModelSet
    {
        public IRecognizer DispatchService { get; set; }

        public Dictionary<string, LuisRecognizer> LuisServices { get; set; } = new Dictionary<string, LuisRecognizer>();

        [Obsolete("Please updated your Virtual Assistant to use the new QnAMakerDialog with Multi Turn and Active Learning support instead. For more information, refer to https://aka.ms/bfvarqnamakerupdate.", false)]
        public Dictionary<string, QnAMaker> QnAServices { get; set; } = new Dictionary<string, QnAMaker>();

        public Dictionary<string, QnAMakerEndpoint> QnAConfiguration { get; set; } = new Dictionary<string, QnAMakerEndpoint>();
    }
}