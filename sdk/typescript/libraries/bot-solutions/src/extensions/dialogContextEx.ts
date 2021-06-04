/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { DialogContext } from 'botbuilder-dialogs';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace DialogContextEx {
    export const suppressDialogCompletionKey = 'suppressDialogCompletionMessage';
    
    /**
     * Provides an extension method to DialogContext enabling a Dialog to indicate whether it wishes to suppress any dialog
     * completion method if for example it's handled itself or the operation (e.g. cancel, repeat) doesn't call for it.
     * @param dc DialogContext
     * @param suppress Boolean indicating whether any automatic dialog completion message should be suppressed.
     */
    export function suppressCompletionMessage(dc: DialogContext, suppress: boolean): void {
        if (dc === undefined) {
            throw new Error('DialogContext is undefined');
        } else {
            dc.context.turnState.set(suppressDialogCompletionKey, suppress);
        }
    }

    /**
     * Provides an extension method to DialogContext enabling the caller to retrieve whether it should suppress a dialog completion message.
     * @param dc DialogContext
     * @returns Indicates whether a dialog completion message should be sent.
     */
    export function suppressCompletionMessageValidation(dc: DialogContext): boolean {
        if (dc === undefined) {
            throw new Error('DialogContext is undefined');
        } else if (dc.context.turnState.has(suppressDialogCompletionKey)) {
            return dc.context.turnState.get(suppressDialogCompletionKey) as boolean;
        } else {
            return false;
        }
    }
}
