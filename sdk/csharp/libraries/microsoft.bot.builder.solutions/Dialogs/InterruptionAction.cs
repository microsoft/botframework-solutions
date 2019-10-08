// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Solutions.Dialogs
{
    /// <summary>
    /// Indicates the current status of a dialog interruption.
    /// </summary>
    public enum InterruptionAction
    {
        /// <summary>
        /// Indicates that the active dialog was interrupted and should end.
        /// </summary>
        End,

        /// <summary>
        /// Indicates that the active dialog was interrupted and needs to resume.
        /// </summary>
        Resume,

        /// <summary>
        /// Indicates that there is a new dialog waiting and the active dialog needs to be shelved.
        /// </summary>
        Waiting,

        /// <summary>
        /// Indicates that no interruption action is required.
        /// </summary>
        NoAction,
    }
}
