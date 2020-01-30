// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Proactive
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class ProactiveModel : Dictionary<string, ProactiveModel.ProactiveData>
    {
        public class ProactiveData
        {
            public ConversationReference Conversation { get; set; }
        }
    }
}