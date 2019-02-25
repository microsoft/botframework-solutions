// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs
{
    public enum InterruptionAction
    {
        /// <summary>
        /// Indicates that the active dialog was interrupted and needs to resume.
        /// </summary>
        MessageSentToUser,

        /// <summary>
        /// Indicates that there is a new dialog waiting and the active dialog needs to be shelved.
        /// </summary>
        StartedDialog,

        /// <summary>
        /// Indicates that no interruption action is required.
        /// </summary>
        NoAction,
    }
}
