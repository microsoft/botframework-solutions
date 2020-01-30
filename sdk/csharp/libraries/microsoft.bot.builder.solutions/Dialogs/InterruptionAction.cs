// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Dialogs
{
    /// <summary>
    /// Indicates the current status of a dialog interruption.
    /// </summary>
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
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
