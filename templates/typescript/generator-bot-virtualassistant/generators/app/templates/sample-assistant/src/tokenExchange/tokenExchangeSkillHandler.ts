/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import {
    Activity,
    ActivityHandlerBase,
    ActivityTypes,
    BotAdapter,
    ExtendedUserTokenProvider,
    ResourceResponse,
    TurnContext,
    SkillConversationReference,
    InvokeResponse,
    SkillConversationIdFactoryBase } from 'botbuilder-core';
import {
    AuthenticationConfiguration,
    ClaimsIdentity,
    JwtTokenValidation,
    SimpleCredentialProvider } from 'botframework-connector';
import { ITokenExchangeConfig } from './';
import { ActivityEx, SkillsConfiguration, IEnhancedBotFrameworkSkill } from 'bot-solutions';
import { SkillHandler, SkillHttpClient, BotFrameworkSkill, BotFrameworkAdapter } from 'botbuilder';
import { tokenExchangeOperationName } from 'botbuilder-core';
import { OAuthCard, Attachment } from 'botframework-schema';
import { uuid } from '../utils';
import { IBotSettings } from '../services';

export class TokenExchangeSkillHandler extends SkillHandler {
    private readonly tokenExchangeSkillHandlerAdapter: BotAdapter;
    private readonly tokenExchangeProvider: ExtendedUserTokenProvider;
    private readonly tokenExchangeConfig: ITokenExchangeConfig;
    private readonly skillsConfig: SkillsConfiguration;
    private readonly skillClient: SkillHttpClient;
    private readonly botId: string;
    private readonly tokenExchangeSkillHandlerConversationIdFactory: SkillConversationIdFactoryBase;
    private readonly oAuthCardContentType: string = 'application/vnd.microsoft.card.oauth';

    public constructor(
        adapter: BotAdapter,
        bot: ActivityHandlerBase,
        configuration: IBotSettings,
        conversationIdFactory: SkillConversationIdFactoryBase,
        skillsConfig: SkillsConfiguration,
        skillClient: SkillHttpClient,
        credentialProvider: SimpleCredentialProvider,
        authConfig: AuthenticationConfiguration,
        tokenExchangeConfig: ITokenExchangeConfig,
        channelService = ''
    ) {
        super(adapter, bot, conversationIdFactory, credentialProvider, authConfig, channelService);
        this.tokenExchangeSkillHandlerAdapter = adapter;
        this.tokenExchangeProvider = adapter as BotFrameworkAdapter;
        this.tokenExchangeConfig = tokenExchangeConfig;
        this.skillsConfig = skillsConfig;
        this.skillClient = skillClient;
        this.tokenExchangeSkillHandlerConversationIdFactory = conversationIdFactory;

        this.botId = configuration.microsoftAppId;
    }

    protected async onSendToConversation(claimsIdentity: ClaimsIdentity, conversationId: string, activity: Activity): Promise<ResourceResponse> {
        if (this.tokenExchangeConfig !== undefined && await this.interceptOAuthCards(claimsIdentity, activity)) {
            return { id: uuid().toString() };
        }

        return await super.onSendToConversation(claimsIdentity, conversationId, activity);
    }

    protected async onReplyToActivity(claimsIdentity: ClaimsIdentity, conversationId: string, activityId: string, activity: Activity): Promise<ResourceResponse> {
        if (this.tokenExchangeConfig !== undefined && await this.interceptOAuthCards(claimsIdentity, activity)) {
            return { id: uuid().toString() };
        }

        return await super.onReplyToActivity(claimsIdentity, conversationId, activityId, activity);
    }

    private getCallingSkill(claimsIdentity: ClaimsIdentity): BotFrameworkSkill | undefined {
        const appId: string = JwtTokenValidation.getAppIdFromClaims(claimsIdentity.claims);

        if (appId === undefined || appId.trim().length === 0) {
            return undefined;
        }

        return Array.from(this.skillsConfig.skills.values())
            .find((s: IEnhancedBotFrameworkSkill) => {
                return s.appId.toLowerCase() === appId.toLowerCase();
            });
    }

    private async interceptOAuthCards(claimsIdentity: ClaimsIdentity, activity: Activity): Promise<boolean> {
        if (activity.attachments !== undefined) {
            let targetSkill: BotFrameworkSkill | undefined;
            activity.attachments.filter(a => a.contentType === this.oAuthCardContentType).forEach(async (attachment: Attachment) => {
                if (targetSkill === undefined) {
                    targetSkill = this.getCallingSkill(claimsIdentity);
                }

                if (targetSkill !== undefined) {
                    const oauthCard: OAuthCard = attachment.content;
                    
                    if (oauthCard !== undefined && oauthCard.tokenExchangeResource !== undefined &&
                        this.tokenExchangeConfig !== undefined && this.tokenExchangeConfig.provider !== undefined && this.tokenExchangeConfig.provider.trim().length > 0 &&
                        this.tokenExchangeConfig.provider === oauthCard.tokenExchangeResource.providerId) {
                        const context: TurnContext = new TurnContext(this.tokenExchangeSkillHandlerAdapter, activity);

                        context.turnState.set(this.tokenExchangeSkillHandlerAdapter.BotIdentityKey, claimsIdentity);

                        // AAD token exchange
                        const result = await this.tokenExchangeProvider.exchangeToken(
                            context,
                            this.tokenExchangeConfig.connectionName,
                            activity.recipient?.id,
                            { uri: oauthCard.tokenExchangeResource.uri });

                        if (result.token !== undefined && result.token.trim().length > 0){
                            // Send an Invoke back to the Skill
                            return await this.sendTokenExchangeInvokeToSkill(activity, oauthCard.tokenExchangeResource.id as string, result.token, oauthCard.connectionName, targetSkill);
                        }

                        return false;
                    }
                }
            });
        }
        
        return false;
    }

    private async sendTokenExchangeInvokeToSkill(incomingActivity: Activity, id: string, token: string, connectionName: string, targetSkill: BotFrameworkSkill): Promise<boolean> {
        const activity: Activity = ActivityEx.createReply(incomingActivity);
        activity.type = ActivityTypes.Invoke;
        activity.name = tokenExchangeOperationName;
        activity.value = {
            id: id,
            token: token,
            connectionName: connectionName
        };

        const conversationReference: SkillConversationReference = await this.tokenExchangeSkillHandlerConversationIdFactory.getSkillConversationReference(incomingActivity.conversation.id);
        activity.conversation = conversationReference.conversationReference.conversation;

        // route the activity to the skill
        const response: InvokeResponse = await this.skillClient.postActivity(this.botId, targetSkill.appId, targetSkill.skillEndpoint, this.skillsConfig.skillHostEndpoint, activity.conversation.id, activity);

        // Check response status: true if success, false if failure
        return response.status >= 200 && response.status <= 299;
    }
}
