// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Data
{
    using System.Threading;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;

    public class UserDataAccessors
    {
        public IStatePropertyAccessor<UserData> UserDataState { get; set; }

        public SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1, 1);
    }
}
