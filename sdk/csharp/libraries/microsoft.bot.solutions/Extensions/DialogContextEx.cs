// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Solutions.Extensions
{
    public static class DialogContextEx
    {
        private const string SuppressDialogCompletionKey = "SuppressDialogCompletionMessage";

        /// <summary>
        /// Provides an extension method to DialogContext enabling a Dialog to indicate whether it wishes to suppress any dialog
        /// completion method if for example it's handled itself or the operation (e.g. cancel, repeat) doesn't call for it.
        /// </summary>
        /// <param name="dc">DialogContext.</param>
        /// <param name="suppress">Boolean indicating whether any automatic dialog completion message should be suppressed.</param>
        public static void SuppressCompletionMessage(this DialogContext dc, bool suppress)
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(DialogContext));
            }
            else
            {
                dc.Context.TurnState[SuppressDialogCompletionKey] = suppress;
            }
        }

        /// <summary>
        /// Provides an extension method to DialogContext enabling the caller to retrieve whether it should suppress a dialog completion message.
        /// </summary>
        /// <param name="dc">DialogContext.</param>
        /// <returns>Indicates whether a dialog completion message should be sent.</param>
        public static bool SuppressCompletionMessage(this DialogContext dc)
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(DialogContext));
            }
            else
            {
                if (dc.Context.TurnState.ContainsKey(SuppressDialogCompletionKey))
                {
                    return (bool)dc.Context.TurnState[SuppressDialogCompletionKey];
                }
                else
                {
                    return false;
                }
            }
        }
    }
}