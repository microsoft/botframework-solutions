/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder-core';
import { Dialog, DialogContext, DialogTurnResult, DialogTurnStatus } from 'botbuilder-dialogs';
import { Activity, ActivityTypes } from 'botframework-schema';
import { ActivityExtensions } from '../extensions';
import { InterruptableDialog } from './interruptableDialog';
import { InterruptionAction } from './interruptionAction';

export abstract class RouterDialog extends InterruptableDialog {
    // Constructor
    constructor(dialogId: string, telemetryClient: BotTelemetryClient) {
        super(dialogId, telemetryClient);
    }

    protected async onBeginDialog(innerDc: DialogContext, options: object): Promise<DialogTurnResult> {
        return this.onContinueDialog(innerDc);
    }

    protected async onContinueDialog(innerDc: DialogContext): Promise<DialogTurnResult> {
        const status: InterruptionAction = await this.onInterruptDialog(innerDc);

        if (status === InterruptionAction.MessageSentToUser) {
            // Resume the waiting dialog after interruption
            await innerDc.repromptDialog();

            return Dialog.EndOfTurn;
        } else if (status === InterruptionAction.StartedDialog) {
            // Stack is already waiting for a response, shelve inner stack
            return Dialog.EndOfTurn;
        } else {
            const activity: Activity = innerDc.context.activity;

            if (ActivityExtensions.isStartActivity(activity)) {
                await this.onStart(innerDc);
            }

            switch (activity.type) {
                case ActivityTypes.Message: {
                    // Note: This check is a workaround for adaptive card buttons that should map to an event
                    // (i.e. startOnboarding button in intro card)
                    if (activity.value) {
                        await this.onEvent(innerDc);
                    } else if (activity.text !== undefined && activity.text !== '') {
                        const result: DialogTurnResult = await innerDc.continueDialog();
                        switch (result.status) {
                            case DialogTurnStatus.empty: {
                                await this.route(innerDc);
                                break;
                            }
                            case DialogTurnStatus.complete: {
                                await this.complete(innerDc, result);
                                // End active dialog
                                await innerDc.endDialog();
                                break;
                            }
                            default:
                        }
                    }
                    break;
                }
                case ActivityTypes.Event: {
                    await this.onEvent(innerDc);
                    break;
                }
                default: {
                    await this.onSystemMessage(innerDc);
                }
            }

            return Dialog.EndOfTurn;
        }
    }

    /**
     * Called when the inner dialog stack is empty.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected abstract route(innerDc: DialogContext): Promise<void>;

    /**
     * Called when the inner dialog stack is complete.
     * @param innerDC - The dialog context for the component.
     * @param result - The dialog result when inner dialog completed.
     * @returns A Promise representing the asynchronous operation.
     */
    protected async complete(innerDc: DialogContext, result: DialogTurnResult): Promise<void> {
        await innerDc.endDialog(result);

        return Promise.resolve();
    }

    /**
     * Called when an event activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected onEvent(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a system activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected onSystemMessage(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a conversation update activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected onStart(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    protected onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {
        return Promise.resolve(InterruptionAction.NoAction);
    }
}
