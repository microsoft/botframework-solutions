// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace VirtualAssistantSample.FunctionalTests.Configuration
{
    public interface IBotTestConfiguration
    {
        string BotId { get; }

        string DirectLineSecret { get; }
    }
}
