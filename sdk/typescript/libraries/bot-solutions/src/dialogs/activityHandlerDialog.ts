/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder-core';
import { InterruptableDialog } from './interruptableDialog';
import { DialogContext, Dialog, DialogTurnStatus, DialogTurnResult } from 'botbuilder-dialogs';
import { InterruptionAction } from './interruptionAction';
import { Activity, ActivityTypes } from 'botbuilder';

/** 
 * Provides interruption logic and methods for handling incoming activities based on type.
 */ 
// OBSOLETE: ActivityHandlerDialog is being deprecated. For more information, refer to https://aka.ms/bfvarouting.
export abstract class ActivityHandlerDialog extends InterruptableDialog {

    public constructor(dialogId: string, telemetryClient: BotTelemetryClient ) {
        super(dialogId, telemetryClient);

        this.telemetryClient = telemetryClient;
    }

    /**
     * Called when the dialog is started and pushed onto the parent's dialog stack.
     * @param innerDc The inner for the current turn of conversation
     * @param options Optional, initial information to pass to the dialog.
     * @returns A promise representing the asynchronous operation
     * @remarks If the task is successful, the result indicates whether the dialog is still 
     * active after the turn has been processed by the dialog. The result may also contain a return value.
     */
    protected async onBeginDialog(innerDc: DialogContext, options: Object): Promise<DialogTurnResult> {
        return this.onContinueDialog(innerDc);
    }

    /**
     * Called when the dialog is continued, where it is the active dialog and the user replies with a new activity.
     * @param innerDc The inner for the current turn of conversation
     * @returns A promise representing the asynchronous operation
     * @remarks If the task is successful, the result indicates whether the dialog is still 
     * active after the turn has been processed by the dialog. The result may also contain a return value.
     * 
     * By default, this calls OnInterruptDialog method then routes the activity to the waiting active dialog,
     * or to a handling method based on its activity type.
     */    
    protected async onContinueDialog(innerDc: DialogContext): Promise<DialogTurnResult> {
        // Check for any interruptions.
        const status = await this.onInterruptDialog(innerDc);

        if (status === InterruptionAction.Resume) {
            // Interruption message was sent, and the waiting dialog should resume/reprompt.
            await innerDc.repromptDialog();
        } else if (status === InterruptionAction.Waiting) {
            // Interruption intercepted conversation and is waiting for user to respond.
            return Dialog.EndOfTurn;
        } else if (status === InterruptionAction.End) {
            // Interruption ended conversation, and current dialog should end.
            return await innerDc.endDialog();
        } else if (status === InterruptionAction.NoAction) {
            // No interruption was detected. Process activity normally.
            const activity: Activity = innerDc.context.activity;
            
            switch (activity.type) {
                case ActivityTypes.Message: {
                    // Pass message to waiting child dialog.
                    const result: DialogTurnResult = await innerDc.continueDialog();

                    if (result.status == DialogTurnStatus.empty) {
                        // There was no waiting dialog on the stack, process message normally.    
                        await this.onMessageActivity(innerDc);
                    }

                    break;
                }
                case ActivityTypes.Event: {
                    await this.onEventActivity(innerDc);
                    break;
                }
                case ActivityTypes.Invoke: {
                    // Used by Teams for Authentication scenarios.
                    await innerDc.continueDialog();
                    break;
                }
                case ActivityTypes.ConversationUpdate: {
                  
                    await this.onMembersAdded(innerDc);
                    break;
                }
                default: {
                    // All other activity types will be routed here. Custom handling should be added in implementation.
                    await this.onUnhandledActivityType(innerDc);
                    break;
                }
            }
        }
        if (innerDc.activeDialog == null)
        {
            // If the inner dialog stack completed during this turn, this component should be ended.
            return await innerDc.endDialog();
        }

        return Dialog.EndOfTurn;
    }

    protected async endComponent(outerDc: DialogContext, result: Object): Promise<DialogTurnResult> {
        // This happens when an inner dialog ends. Could call complete here
        await this.onDialogComplete(outerDc, result);
        return await super.endComponent(outerDc, result);
    }

    /**
     * Called on every turn, enabling interruption scenarios.
     * @param innerDc The dialog context for the component.
     * @returns A promise returning an InterruptionAction which indicates what action should be taken after interruption.
     */
    protected async onInterruptDialog(innerDc: DialogContext): Promise<InterruptionAction> {
        return Promise.resolve(InterruptionAction.NoAction);
    }

    /**
     * Called when an event activity is received.
     * @param innerDc The dialog context for the component.
     * @returns A promise representing the asynchronous operation.
     */
    protected async onEventActivity(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a message activity is received.
     * @param innerDc The dialog context for the component.
     * @returns A promise representing the asynchronous operation.
     */   
    protected async onMessageActivity(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when a conversationUpdate activity is received.
     * @param innerDc The dialog context for the component.
     * @returns A promise representing the asynchronous operation.
     */
    protected async onMembersAdded(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when an activity type other than event, message, or conversationUpdate is received.
     * @param innerDc The dialog context for the component.
     * @returns A promise representing the asynchronous operation.
     */
    protected async onUnhandledActivityType(innerDc: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    /**
     * Called when the inner dialog stack completes.
     * @param innerDc The dialog context for the component.
     * @param result The dialog turn result for the component.
     * @returns A promise representing the asynchronous operation.
     */
    protected async onDialogComplete(outerDc: DialogContext, result: object): Promise<void> {
        return Promise.resolve();
    }
}
