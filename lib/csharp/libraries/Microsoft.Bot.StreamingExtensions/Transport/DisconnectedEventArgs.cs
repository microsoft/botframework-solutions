// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.StreamingExtensions.Transport
{
    /// <summary>
    /// Arguments to be included when disconnection events are fired.
    /// </summary>
    public class DisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets an empty set of arguments.
        /// </summary>
        /// <value>
        /// A new, empty, set of <see cref="DisconnectedEventArgs"/>.
        /// </value>
        public static new DisconnectedEventArgs Empty { get; set; } = new DisconnectedEventArgs();

        /// <summary>
        /// Gets or sets the reason field of the arguments.
        /// </summary>
        /// <value>
        /// The reason the disconnection event fired, in plain text.
        /// </value>
        public string Reason { get; set; }
    }
}
