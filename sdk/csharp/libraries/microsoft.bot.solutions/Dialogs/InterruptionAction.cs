// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Solutions.Dialogs
{
    /// <summary>
    /// Indicates the current status of a dialog interruption.
    /// </summary>
    [Obsolete("This class is being deprecated. For more information, refer to https://aka.ms/bfvarouting.", false)]
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
