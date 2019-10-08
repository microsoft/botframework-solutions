// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using System;
using VirtualAssistant.Services;

namespace VirtualAssistant.Proactive
{
    public class ProactiveState : BotState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProactiveState"/> class.</summary>
        /// <param name="storage">The storage provider to use.</param>
        public ProactiveState(BotSettings botSettings)
            : base(new CosmosDbStorage(botSettings.CosmosDbProactive), nameof(ProactiveState))
        {
        }

        /// <summary>Gets the storage key for caching state information.</summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>The storage key.</returns>
        protected override string GetStorageKey(ITurnContext turnContext)
        {
            var channelId = turnContext.Activity.ChannelId ?? throw new ArgumentNullException("invalid activity-missing channelId");
            var userId = turnContext.Activity.From?.Id ?? throw new ArgumentNullException("invalid activity-missing From.Id");
            return $"proactive/{channelId}/users/{userId}";
        }
    }
}