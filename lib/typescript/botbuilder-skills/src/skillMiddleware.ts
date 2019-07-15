/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConversationState, Middleware, StatePropertyAccessor, TurnContext } from 'botbuilder';
import { DialogState } from 'botbuilder-dialogs';
import { Activity, ActivityTypes } from 'botframework-schema';
import { SkillEvents } from './models';

/**
 * The Skill middleware is responsible for processing Skill mode specifics,
 * for example the skillBegin event used to signal the start of a skill conversation.
 */
export class SkillMiddleware implements Middleware {
    private readonly conversationState: ConversationState;
    private readonly dialogStateAccessor: StatePropertyAccessor<DialogState>;

    public constructor(
        conversationState: ConversationState,
        dialogStateAccessor: StatePropertyAccessor<DialogState>
    ) {
        this.conversationState = conversationState;
        this.dialogStateAccessor = dialogStateAccessor;
    }

    public async onTurn(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        const activity: Activity = turnContext.activity;

        if (activity !== undefined && activity.type === ActivityTypes.Event) {
            if (activity.name === SkillEvents.cancelAllSkillDialogsEventName) {

                // when skill receives a CancelAllSkillDialogsEvent, clear the dialog stack and short-circuit
                const currentConversation: DialogState|undefined = await this.dialogStateAccessor.get(turnContext, { dialogStack: [] });
                if (currentConversation !== undefined) {
                    currentConversation.dialogStack = [];
                    await this.dialogStateAccessor.set(turnContext, currentConversation);
                    await this.conversationState.saveChanges(turnContext, true);
                }

                return;
            }
        }

        await next();
    }
}
