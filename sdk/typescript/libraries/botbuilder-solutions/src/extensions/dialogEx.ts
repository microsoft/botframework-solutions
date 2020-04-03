
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { Dialog, DialogState, DialogSet, DialogContext, DialogTurnResult, DialogTurnStatus, DialogEvents } from "botbuilder-dialogs";
import { TurnContext, StatePropertyAccessor, ActivityTypes, Activity } from "botbuilder";
import { ClaimsIdentity, SkillValidation } from "botframework-connector";

export namespace DialogEx {
    export async function run(dialog: Dialog, turnContext: TurnContext, accessor: StatePropertyAccessor<DialogState>): Promise<void> {
        const dialogSet: DialogSet = new DialogSet(accessor);
        dialogSet.telemetryClient = dialog.telemetryClient;
        dialogSet.add(dialog);

        const dialogContext: DialogContext = await dialogSet.createContext(turnContext);
        const botIdentity = turnContext.turnState.get((turnContext.adapter as any).BotIdentityKey);
        
        if (botIdentity instanceof ClaimsIdentity && SkillValidation.isSkillClaim(botIdentity.claims)) {
            // The bot is running as a skill.
            if (turnContext.activity.type === ActivityTypes.EndOfConversation && dialogContext.stack.length > 0 && isEocComingFromParent(turnContext)) {
                // Handle remote cancellation request from parent.
                const activeDialogContext: DialogContext =  getActiveDialogContext(dialogContext);

                const remoteCancelText: string = "Skill was canceled through an EndOfConversation activity from the parent.";
                await turnContext.sendActivity({
                    type: ActivityTypes.Trace,
                    name: `${Dialog.name}.run()`,
                    label: remoteCancelText
                })

                // Send cancellation message to the top dialog in the stack to ensure all the parents are canceled in the right order.
                await activeDialogContext.cancelAllDialogs(true);
            } else {
                // Process a reprompt event sent from the parent.
                if (turnContext.activity.type === ActivityTypes.Event && turnContext.activity.name === DialogEvents.repromptDialog && dialogContext.stack.length > 0) {
                    await dialogContext.repromptDialog();
                    return;
                }

                // Run the Dialog with the new message Activity and capture the results so we can send end of conversation if needed.
                let result: DialogTurnResult = await dialogContext.continueDialog();
                if (result.status === DialogTurnStatus.empty) {
                    const startMessageText: string = `Starting ${dialog.id}.`;
                    await turnContext.sendActivity({
                        type: ActivityTypes.Trace,
                        name: `${Dialog.name}.run()`,
                        label: startMessageText
                    })
                    result = await dialogContext.beginDialog(dialog.id);
                }

                // Send end of conversation if it is completed or cancelled.
                if (result.status === DialogTurnStatus.complete || result.status === DialogTurnStatus.cancelled) {
                    const endMessageText: string = `Dialog ${dialog.id} has **completed**. Sending EndOfConversation.`;
                    await turnContext.sendActivity({
                        type: ActivityTypes.Trace,
                        name: `${Dialog.name}.run()`,
                        label: endMessageText
                    })

                    // Send End of conversation at the end.
                    const activity: Partial<Activity> = {
                        type: ActivityTypes.EndOfConversation,
                        value: result.result
                    }
                    await turnContext.sendActivity(activity);
                }
            }
        } else {
            // The bot is running as a standard bot.
            const results: DialogTurnResult = await dialogContext.continueDialog();
            if (results.status === DialogTurnStatus.empty) {
                await dialogContext.beginDialog(dialog.id);
            }
        }
    }

    // We should only cancel the current dialog stack if the EoC activity is coming from a parent (a root bot or another skill).
    // When the EoC is coming back from a child, we should just process that EoC normally through the 
    // dialog stack and let the child dialogs handle that.
    export function isEocComingFromParent(turnContext: TurnContext): boolean {
        // To determine the direction we check callerId property which is set to the parent bot
        // by the BotFrameworkHttpClient on outgoing requests.
        return turnContext.activity.callerId !== undefined && turnContext.activity.callerId.trim().length > 0;
    }

    // Recursively walk up the DC stack to find the active DC.
    export function getActiveDialogContext(dialogContext: DialogContext): DialogContext {
        const child: DialogContext | undefined = dialogContext.child;
        if (child === undefined) {
            return dialogContext;
        }

        return getActiveDialogContext(child);
    } 
}
