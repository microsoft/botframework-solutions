/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ResourceResponse, SkillConversationReference, TurnContext, BotFrameworkAdapter, BotAdapter, SkillConversationIdFactoryBase, ChannelServiceHandler } from "botbuilder";
import { ClaimsIdentity, JwtTokenValidation, GovernmentConstants, AuthenticationConstants, AppCredentials, ICredentialProvider, AuthenticationConfiguration } from "botframework-connector";
import { SkillConversationReferenceKey } from 'botbuilder-core';

export class TestSkillHandler extends ChannelServiceHandler {
    public readonly SkillConversationReferenceKey: symbol = SkillConversationReferenceKey;
    private readonly adapter: BotAdapter;
    private readonly conversationIdFactory: SkillConversationIdFactoryBase;

    public constructor(
        adapter: BotAdapter,
        conversationIdFactory: SkillConversationIdFactoryBase,
        credentialProvider: ICredentialProvider,
        authConfig: AuthenticationConfiguration,
        channelService?: string
    ){
        super(credentialProvider, authConfig, channelService);
        if (!conversationIdFactory) {
            throw new Error('missing conversationIdFactory.');
        }
        this.adapter = adapter;
        this.conversationIdFactory = conversationIdFactory;
    }

    protected async onUpdateActivity(claimsIdentity: ClaimsIdentity, conversationId: string, activityId: string, activity: Activity): Promise<ResourceResponse> {
        return await this.updateActivity(claimsIdentity, conversationId, activityId, activity);
    }

    private async updateActivity(claimsIdentity: ClaimsIdentity, conversationId: string, replyToActivityId: string, activity: Activity): Promise<ResourceResponse> {

        let skillConversationReference: SkillConversationReference;
        try {
            skillConversationReference = await this.conversationIdFactory.getSkillConversationReference(conversationId);
        } catch (err) {
            // If the factory has overridden getSkillConversationReference, call the deprecated getConversationReference().
            // In this scenario, the oAuthScope paired with the ConversationReference can only be used for talking with
            // an official channel, not another bot.
            if (err.message === 'Not Implemented') {
                const conversationReference = await this.conversationIdFactory.getConversationReference(conversationId);
                skillConversationReference = {
                    conversationReference,
                    oAuthScope: JwtTokenValidation.isGovernment(this.channelService) ?
                        GovernmentConstants.ToChannelFromBotOAuthScope :
                        AuthenticationConstants.ToChannelFromBotOAuthScope
                };
            } else {
                // Re-throw all other errors. 
                throw err;
            }
        }

        if (!skillConversationReference) {
            throw new Error('skillConversationReference not found');
        }
        if (!skillConversationReference.conversationReference) {
            throw new Error('conversationReference not found.');
        }

        const activityConversationReference = TurnContext.getConversationReference(activity);

        /**
         * Callback passed to the BotFrameworkAdapter.createConversation() call.
         * This function does the following:
         *  - Caches the ClaimsIdentity on the TurnContext.turnState
         *  - Applies the correct ConversationReference to the Activity for sending to the user-router conversation.
         *  - For EndOfConversation Activities received from the Skill, removes the ConversationReference from the
         *    ConversationIdFactory
         */
        const callback = async (context: TurnContext): Promise<void> => {
            const adapter: BotFrameworkAdapter = (context.adapter as BotFrameworkAdapter);
            // Cache the ClaimsIdentity and ConnectorClient on the context so that it's available inside of the bot's logic.
            context.turnState.set(adapter.BotIdentityKey, claimsIdentity);
            context.turnState.set(this.SkillConversationReferenceKey, activityConversationReference);
            activity = TurnContext.applyConversationReference(activity, skillConversationReference.conversationReference) as Activity;
            const client = adapter.createConnectorClient(activity.serviceUrl);
            context.turnState.set(adapter.ConnectorClientKey, client);

            context.activity.id = replyToActivityId;
            await context.updateActivity(context.activity);
            return;
        };

        // Add the channel service URL to the trusted services list so we can send messages back.
        // the service URL for skills is trusted because it is applied based on the original request
        // received by the root bot.
        AppCredentials.trustServiceUrl(skillConversationReference.conversationReference.serviceUrl);

        await (this.adapter as BotFrameworkAdapter).continueConversation(skillConversationReference.conversationReference, skillConversationReference.oAuthScope, callback);
        return { id: uuid() };
    }
}

// Code is from @stevenic: https://github.com/stevenic
function uuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c): string => {
        const r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}
