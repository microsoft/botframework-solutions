// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.StreamingExtensions.Transport
{
    /// <summary>
    /// Delegate used to setup actions to be taken when disconnection events are triggered.
    /// </summary>
    /// <param name="sender">The source of the disconnection event.</param>
    /// <param name="e">The arguments specified by the disconnection event.</param>
    public delegate void DisconnectedEventHandler(object sender, DisconnectedEventArgs e);
}
