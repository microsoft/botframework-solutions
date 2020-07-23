
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { Dialog, DialogState, DialogSet, DialogContext, DialogTurnResult, DialogTurnStatus, DialogEvents } from 'botbuilder-dialogs';
import { TurnContext, StatePropertyAccessor, ActivityTypes, Activity, SkillConversationReferenceKey, SkillConversationReference } from 'botbuilder';
import { ClaimsIdentity, SkillValidation, AuthenticationConstants, GovernmentConstants } from 'botframework-connector';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace DialogEx {
    /**
     * Creates a dialog stack and starts a dialog, pushing it onto the stack.
     * @param dialog The dialog to start.
     * @param turnContext The context for the current turn of the conversation
     * @param accessor The IStatePropertyAccessor{DialogState} accessor
     * with which to manage the state of the dialog stack.
     * @returns A Promise representing the asynchronous operation.
     */
    export async function run(dialog: Dialog, turnContext: TurnContext, accessor: StatePropertyAccessor<DialogState>): Promise<void> {
        const dialogSet: DialogSet = new DialogSet(accessor);
        dialogSet.telemetryClient = dialog.telemetryClient;
        dialogSet.add(dialog);

        const dialogContext: DialogContext = await dialogSet.createContext(turnContext);

        // Handle EoC and Reprompt event from a parent bot (can be root bot to skill or skill to skill)
        if (isFromParentToSkill(turnContext)) {
            // Handle remote cancellation request from parent.
            if (turnContext.activity.type === ActivityTypes.EndOfConversation) {
                if(dialogContext.stack.length === 0) {
                    // No dialogs to cancel, just return.
                    return;
                }

                const activeDialogContext: DialogContext =  getActiveDialogContext(dialogContext);

                const remoteCancelText = 'Skill was canceled through an EndOfConversation activity from the parent.';
                await turnContext.sendActivity({
                    type: ActivityTypes.Trace,
                    name: `${ Dialog.name }.run()`,
                    label: remoteCancelText
                });

                // Send cancellation message to the top dialog in the stack to ensure all the parents are canceled in the right order.
                await activeDialogContext.cancelAllDialogs(true);

                return;
            }

            // / Handle a reprompt event sent from the parent.
            if (turnContext.activity.type === ActivityTypes.Event && turnContext.activity.name === DialogEvents.repromptDialog) {
                if(dialogContext.stack.length === 0) {
                    // No dialogs to cancel, just return.
                    return;
                }

                await dialogContext.repromptDialog();
                return;
            }
        }

        // Continue or start the dialog.
        let result: DialogTurnResult = await dialogContext.continueDialog();
        if (result.status === DialogTurnStatus.empty) {
            result = await dialogContext.beginDialog(dialog.id, undefined);
        }

        // Skills should send EoC when the dialog completes.
        if (result.status === DialogTurnStatus.complete || result.status == DialogTurnStatus.cancelled) {
            if (sendEoCToParent(turnContext)) {
                const endMessageText = `Dialog ${ dialog.id } has **completed**. Sending EndOfConversation.`;
                await turnContext.sendActivity({
                    type: ActivityTypes.Trace,
                    name: `${ Dialog.name }.run()`,
                    label: endMessageText,
                    value: result.result
                });

                // Send End of conversation at the end.
                const activity: Partial<Activity> = {
                    type: ActivityTypes.EndOfConversation,
                    value: result.result,
                    locale:  turnContext.activity.locale
                };
                await turnContext.sendActivity(activity);
            }
        }
    }

    // Helper to determine if we should send an EoC to the parent or not.
    export function sendEoCToParent(turnContext: TurnContext): boolean {
        const botIdentity = turnContext.turnState.get((turnContext.adapter as any).BotIdentityKey);
        if (botIdentity instanceof ClaimsIdentity && SkillValidation.isSkillClaim(botIdentity.claims)) {
            // EoC Activities returned by skills are bounced back to the bot by SkillHandler
            // In those cases we will have a SkillConversationReference instance is state
            const skillConversationReference: SkillConversationReference = turnContext.turnState.get(SkillConversationReferenceKey);
            if (skillConversationReference !== undefined) {
                // If the skillConversationReference.OAuthScope is for one of the supported channels, we are at the root and we should not send an EoC.
                return skillConversationReference.oAuthScope !== AuthenticationConstants.ToChannelFromBotOAuthScope && skillConversationReference.oAuthScope !== GovernmentConstants.ToChannelFromBotOAuthScope;
            }

            return true;
        }

        return false;
    }

    export function isFromParentToSkill(turnContext: TurnContext): boolean {
        if (turnContext.turnState.get(SkillConversationReferenceKey) !== undefined) {
            return false;
        }

        const botIdentity = turnContext.turnState.get((turnContext.adapter as any).BotIdentityKey);
        return botIdentity instanceof ClaimsIdentity && SkillValidation.isSkillClaim(botIdentity.claims);
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
