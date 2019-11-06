// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Solutions.Dialogs
{
    /// <summary>
    /// Define router dialog turn result.
    /// </summary>
    public class RouterDialogTurnResult
    {
        public RouterDialogTurnResult(RouterDialogTurnStatus status)
        {
            this.Status = status;
        }

        public RouterDialogTurnStatus Status { get; set; }
    }
}
