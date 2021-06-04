// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Proactive
{
    [ExcludeFromCodeCoverageAttribute]
    public class ProactiveModel : Dictionary<string, ProactiveModel.ProactiveData>
    {
        public class ProactiveData
        {
            public ConversationReference Conversation { get; set; }
        }
    }
}