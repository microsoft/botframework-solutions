// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Models.Proactive
{
    public class ProactiveModel : Dictionary<string, ProactiveModel.ProactiveData>
    {
        public class ProactiveData
        {
            public ConversationReference Conversation { get; set; }
        }
    }
}