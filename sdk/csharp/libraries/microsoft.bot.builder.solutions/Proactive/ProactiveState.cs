// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Builder.Solutions.Proactive
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class ProactiveState : BotState
    {
        /// <summary>The key used to cache the state information in the turn context.</summary>
        private const string StorageKey = "ProactiveState";

        /// <summary>
        /// Initializes a new instance of the <see cref="ProactiveState"/> class.</summary>
        /// <param name="storage">The storage provider to use.</param>
        public ProactiveState(IStorage storage)
            : base(storage, StorageKey)
        {
        }

        /// <summary>Gets the storage key for caching state information.</summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>The storage key.</returns>
        protected override string GetStorageKey(ITurnContext turnContext) => StorageKey;
    }
}