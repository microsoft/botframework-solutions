/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ConversationState,
    SetSpeakMiddleware,
    ShowTypingMiddleware,
    SkillHttpClient,
    TranscriptLoggerMiddleware,
    TranscriptStore,
    TurnContext,
    TelemetryException } from 'botbuilder';
import { BlobsTranscriptStore } from 'botbuilder-azure-blobs';
import {
    EventDebuggerMiddleware,
    LocaleTemplateManager,
    SetLocaleMiddleware } from 'bot-solutions';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { IBotSettings } from '../services/botSettings.js';
import { ActivityEx, SkillsConfiguration } from 'bot-solutions/lib';
import { MainDialog } from '../dialogs/mainDialog';
import { Activity, BotFrameworkSkill, IEndOfConversationActivity } from 'botbuilder-core';

export class DefaultAdapter extends BotFrameworkAdapter {

    private readonly conversationState: ConversationState;
    private readonly telemetryClient: BotTelemetryClient;
    private readonly templateEngine: LocaleTemplateManager;
    private readonly skillClient: SkillHttpClient;
    private readonly skillsConfig: SkillsConfiguration;

    public constructor(
        settings: Partial<IBotSettings>,
        templateEngine: LocaleTemplateManager,
        conversationState: ConversationState,
        adapterSettings: Partial<BotFrameworkAdapterSettings>,
        telemetryMiddleware: TelemetryInitializerMiddleware,
        telemetryClient: BotTelemetryClient,
        skillsConfig: SkillsConfiguration,
        skillClient: SkillHttpClient
    ) {
        super(adapterSettings);

        if (conversationState === undefined) {
            throw new Error('conversationState parameter is null');
        }
        this.conversationState = conversationState;
        if (templateEngine === undefined) {
            throw new Error('templateEngine parameter is null');
        }
        this.templateEngine = templateEngine;
        if (telemetryClient === undefined) {
            throw new Error('telemetryClient parameter is null');
        }
        this.telemetryClient = telemetryClient;
        this.skillClient = skillClient;
        this.skillsConfig = skillsConfig;

        this.onTurnError = this.handleTurnError;

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        const transcriptStore: TranscriptStore = new BlobsTranscriptStore(settings.blobStorage.connectionString, settings.blobStorage.container);

        this.use(telemetryMiddleware);

        // Uncomment the following line for local development without Azure Storage
        // this.use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
        this.use(new TranscriptLoggerMiddleware(transcriptStore));
        this.use(new ShowTypingMiddleware());
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new SetSpeakMiddleware('en-US-JennyNeural', true));
    }

    private async handleTurnError(turnContext: TurnContext, error: Error): Promise<void> {
        // Log any leaked exception from the application.
        console.error(`[onTurnError] unhandled error : ${ error }`);

        await this.sendErrorMessage(turnContext, error);
        await this.endSkillConversation(turnContext);
        await this.clearConversationState(turnContext);
    }

    private async sendErrorMessage(turnContext: TurnContext, error: Error): Promise<void> {
        try {
            const telemetryException: TelemetryException = {
                exception: error
            };

            this.telemetryClient.trackException(telemetryException);

            // Send a message to the user.
            await turnContext.sendActivity(this.templateEngine.generateActivityForLocale('ErrorMessage', turnContext.activity.locale));

            // Send a trace activity, which will be displayed in the Bot Framework Emulator.
            // Note: we return the entire exception in the value property to help the developer;
            // this should not be done in production.
            await turnContext.sendTraceActivity('onTurnError Trace', error.message, 'https://www.botframework.com/schemas/error', 'TurnError');
        }
        catch (err) {
            console.error(`Exception caught in sendErrorMessage : ${ err }`);
        }
    }

    private async endSkillConversation(turnContext: TurnContext): Promise<void> {
        if (this.skillClient === undefined || this.skillsConfig === undefined) {
            return;
        }

        try {
            // Inform the active skill that the conversation is ended so that it has a chance to clean up.
            // Note: the root bot manages the ActiveSkillPropertyName, which has a value while the root bot
            // has an active conversation with a skill.
            const activeSkill: BotFrameworkSkill | undefined = await this.conversationState.createProperty<BotFrameworkSkill>(MainDialog.activeSkillPropertyName).get(turnContext);
            if (activeSkill !== undefined) {
                let endOfConversation: Partial<IEndOfConversationActivity> = ActivityEx.createEndOfConversationActivity();
                endOfConversation.code = 'RootSkillError';
                endOfConversation = TurnContext.applyConversationReference(endOfConversation, TurnContext.getConversationReference(turnContext.activity), true);

                await this.conversationState.saveChanges(turnContext, true);
                await this.skillClient.postToSkill(this.settings.appId, activeSkill, this.skillsConfig.skillHostEndpoint, endOfConversation as Activity);
            }
        }
        catch (err) {
            console.error(`Exception caught on attempting to send endOfConversation : ${ err }`);
        }
    }

    private async clearConversationState(turnContext: TurnContext): Promise<void> {
        try {
            // Delete the conversationState for the current conversation to prevent the
            // bot from getting stuck in a error-loop caused by being in a bad state.
            // ConversationState should be thought of as similar to "cookie-state" for a Web page.
            await this.conversationState.delete(turnContext);
        }
        catch (err) {
            console.error(`Exception caught on attempting to Delete ConversationState : ${ err }`);
        }
    }
}
