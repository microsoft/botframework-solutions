// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    /// Interface to express the relationship between the bot and skill or skill to skills.
    /// This interface can be used for Dependency Injection.
    /// </summary>
    public interface ISkillWebSocketAdapter : IBotFrameworkHttpAdapter
    {
    }
}
