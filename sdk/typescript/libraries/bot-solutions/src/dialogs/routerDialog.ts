/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import { BotTelemetryClient } from 'botbuilder-core';
import { Dialog, DialogContext, DialogInstance, DialogReason, DialogTurnResult, DialogTurnStatus } from 'botbuilder-dialogs';
import { Activity, ActivityTypes } from 'botframework-schema';
import { ActivityEx } from '../extensions';
import { InterruptableDialog } from './interruptableDialog';
import { InterruptionAction } from './interruptionAction';

/** 
 * DEPRECATED "Please use ActivityHandlerDialog instead. For more information, refer to https://aka.ms/bfvarouting."
 */ 
export abstract class RouterDialog extends InterruptableDialog {
    public constructor(dialogId: string, telemetryClient: BotTelemetryClient) {
        super(dialogId, telemetryClient);
        this.telemetryClient = telemetryClient;
    }

    protected async onBeginDialog(innerDc: DialogContext, options: Object): Promise<DialogTurnResult> {
        return this.onContinueDialog(innerDc);
    }

    protected async onContinueDialog(innerDc: DialogContext): Promise<DialogTurnResult> {
        const status: InterruptionAction = await this.onInterruptDialog(innerDc);

        if (status === InterruptionAction.Resume) {
            // Resume the waiting dialog after interruption
            await innerDc.repromptDialog();

            return Dialog.EndOfTurn;
        } else if (status === InterruptionAction.Waiting) {
            // Stack is already waiting for a response, shelve inner stack
            return Dialog.EndOfTurn;
        } else {
            const activity: Activity = innerDc.context.activity;

            if (ActivityEx.isStartActivity(activity)) {
                await this.onStart(innerDc);
            }

            switch (activity.type) {
                case ActivityTypes.Message: {
                    // Note: This check is a workaround for adaptive card buttons that should map to an event
                    // (i.e. startOnboarding button in intro card)
                    if (activity.value) {
                        await this.onEvent(innerDc);
                    } else {
                        const result: DialogTurnResult = await innerDc.continueDialog();

                        switch (result.status) {
                            case DialogTurnStatus.empty: {
                                await this.route(innerDc);
                                break;
                            }
                            case DialogTurnStatus.complete: {
                                // End active dialog
                                await innerDc.endDialog();
                                break;
                            }
                            default:
                        }
                    }

                    // If the active dialog was ended on this turn (either on single-turn dialog, or on continueDialogAsync)
                    // run CompleteAsync method.
                    if (innerDc.activeDialog === undefined) {
                        await this.complete(innerDc);
                    }

                    break;
                }
                case ActivityTypes.Event: {
                    await this.onEvent(innerDc);
                    break;
                }
                case ActivityTypes.Invoke: {
                    // Used by Teams for Authentication scenarios.
                    await innerDc.continueDialog();
                    break;
                }
                default: {
                    await this.onSystemMessage(innerDc);
                    break;
                }
            }

            return Dialog.EndOfTurn;
        }
    }

    protected async onEndDialog(context: TurnContext, instance: DialogInstance, reason: DialogReason): Promise<void> {
        return super.onEndDialog(context, instance, reason);
    }

    protected async onRepromptDialog(context: TurnContext, instance: DialogInstance): Promise<void> {
        return super.onRepromptDialog(context, instance);
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
    protected async complete(innerDc: DialogContext, result?: DialogTurnResult): Promise<void> {
        await innerDc.endDialog(result);

        return Promise.resolve();
    }

    /**
     * Called when an event activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected async onEvent(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a system activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected async onSystemMessage(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a conversation update activity is received.
     * @param innerDC - The dialog context for the component.
     * @returns A Promise representing the asynchronous operation.
     */
    protected async onStart(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    protected async onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {
        return Promise.resolve(InterruptionAction.NoAction);
    }
}
