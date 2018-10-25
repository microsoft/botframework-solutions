// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace TestFramework.Extensions
{
    using Microsoft.Bot.Builder.Adapters;

    public static class TestFlowExtensions
    {
        public static TestFlow NextReply(this TestFlow flow)
        {
            return flow.Send(string.Empty);
        }
    }
}