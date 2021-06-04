// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Proactive.Sharded
{
    using Microsoft.Bot.Builder;

    public class ProactiveStateSharded : BotState
    {
        /// <summary>The key used to cache the state information in the turn context.</summary>
        private const string ShardedStorageKey = "ProactiveState_";

        /// <summary>
        /// Initializes a new instance of the <see cref="ProactiveStateSharded"/> class.</summary>
        /// <param name="storage">The storage provider to use.</param>
        /// <param name="turnContext">TurnContext to get conversationId.</param>
        public ProactiveStateSharded(IStorage storage)
            : base(storage, ShardedStorageKey)
        {
        }

        /// <summary>Gets the storage key for caching state information.</summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>The storage key.</returns>
        protected override string GetStorageKey(ITurnContext turnContext) => ShardedStorageKey + turnContext.Activity.Conversation.Id;
    }
}
