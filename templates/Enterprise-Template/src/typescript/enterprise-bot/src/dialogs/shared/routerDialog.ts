// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    Activity,
    ActivityTypes } from 'botbuilder';
import {
    ComponentDialog,
    Dialog,
    DialogContext,
    DialogTurnResult,
    DialogTurnStatus } from 'botbuilder-dialogs';
import { ActivityExtensions } from '../../extensions/activityExtensions';

export abstract class RouterDialog extends ComponentDialog {
    constructor(dialogId: string) { super(dialogId); }

    protected onBeginDialog(dc: DialogContext): Promise<DialogTurnResult> {
        return this.onContinueDialog(dc);
    }

    protected async onContinueDialog(dc: DialogContext): Promise<DialogTurnResult> {
        const activity: Activity = dc.context.activity;
        if (ActivityExtensions.isStartActivity(activity)) {
            await this.onStart(dc);
        }
        switch (activity.type) {
            case ActivityTypes.Message: {
                if (activity.value !== undefined) {
                    await this.onEvent(dc);
                } else if (typeof activity.text !== undefined && activity.text) {
                    const result: DialogTurnResult<any> = await dc.continueDialog();
                    switch (result.status) {
                        case DialogTurnStatus.empty: {
                            await this.route(dc);
                            break;
                        }
                        case DialogTurnStatus.complete: {
                            await this.complete(dc);

                            // End active dialog.
                            await dc.endDialog();
                        }
                        default:
                    }
                }
                break;
            }
            case ActivityTypes.Event: {
                await this.onEvent(dc);
                break;
            }
            default: {
                await this.onSystemMessage(dc);
            }
        }

        return Dialog.EndOfTurn;
    }

    /**
     * Called when the inner dialog stack is empty.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected abstract route(innerDC: DialogContext): Promise<void>;

    /**
     * Called when the inner dialog stack is complete
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected complete(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when an event activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation. 
     */
    protected onEvent(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a system activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A promise representing the asynchronous operation.
     */
    protected onSystemMessage(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a conversation update activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected onStart(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }
}
