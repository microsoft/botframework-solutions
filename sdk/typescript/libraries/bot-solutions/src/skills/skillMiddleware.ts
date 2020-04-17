/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConversationState, Middleware, StatePropertyAccessor, TurnContext, UserState } from 'botbuilder';
import { DialogState } from 'botbuilder-dialogs';
import { Activity, ActivityTypes } from 'botframework-schema';

/**
 * The Skill middleware is responsible for processing Skill mode specifics,
 * for example the skillBegin event used to signal the start of a skill conversation.
 */
export class SkillMiddleware implements Middleware {
    private readonly userState: UserState;
    private readonly conversationState: ConversationState;
    private readonly dialogState: StatePropertyAccessor<DialogState>;

    public constructor(
        userState: UserState,
        conversationState: ConversationState,
        dialogState: StatePropertyAccessor<DialogState>
    ) {
        this.userState = userState;
        this.conversationState = conversationState;
        this.dialogState = dialogState;
    }

    public async onTurn(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        const activity: Activity = turnContext.activity;
        if (activity !== undefined && activity.type === ActivityTypes.EndOfConversation) {
            await this.dialogState.delete(turnContext);
            await this.conversationState.clear(turnContext);
            await this.conversationState.saveChanges(turnContext, true);

            return;
        }

        await next();
    }
}
