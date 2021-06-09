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
    TelemetryException,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TurnContext } from 'botbuilder';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware,
    LocaleTemplateManager,
    ActivityEx } from 'bot-solutions';
import { IBotSettings } from '../services/botSettings';
import { TurnContextEx } from '../extensions/turnContextEx';
import { BlobsTranscriptStore } from 'botbuilder-azure-blobs';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';

export class DefaultAdapter extends BotFrameworkAdapter {

    private readonly conversationState: ConversationState;
    private readonly telemetryClient: BotTelemetryClient;
    private readonly templateEngine: LocaleTemplateManager;

    public constructor(
        settings: Partial<IBotSettings>,
        templateEngine: LocaleTemplateManager,
        conversationState: ConversationState,
        telemetryMiddleware: TelemetryInitializerMiddleware,
        telemetryClient: BotTelemetryClient,
        adapterSettings: Partial<BotFrameworkAdapterSettings>
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

        this.onTurnError = this.handleTurnError;

        this.use(telemetryMiddleware);

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }
        
        // Uncomment the following line for local development without Azure Storage
        // this.use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
        this.use(new TranscriptLoggerMiddleware(new BlobsTranscriptStore(settings.blobStorage.connectionString, settings.blobStorage.container)));
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new ShowTypingMiddleware());
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new SetSpeakMiddleware('en-US-JennyNeural', true));
    }

    private async handleTurnError(turnContext: TurnContext, error: Error): Promise<void> {
        // Log any leaked exception from the application.
        console.error(`[onTurnError] unhandled error : ${ error }`);

        await this.sendErrorMessage(turnContext, error);
        await this.sendEndOfConversationToParent(turnContext, error);
        await this.clearConversationStateAsync(turnContext);
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
            await turnContext.sendTraceActivity('OnTurnError Trace', error.message, 'https://www.botframework.com/schemas/error', 'TurnError');
        }
        catch (err) {
            console.error(`Exception caught in sendErrorMessage : ${ err }`);
        }
    }

    private async sendEndOfConversationToParent(turnContext: TurnContext, error: Error): Promise<void> {
        try {
            if (TurnContextEx.isSkill(turnContext)) {
                // Send and EndOfConversation activity to the skill caller with the error to end the conversation
                // and let the caller decide what to do.
                const endOfConversation = ActivityEx.createEndOfConversationActivity();
                endOfConversation.code = 'SkillError';
                endOfConversation.text = error.message;
                await turnContext.sendActivity(endOfConversation);
            }
        }
        catch (err) {
            console.error(`Exception caught in sendEoCToParent : ${ err }`);
        }
    }

    private async clearConversationStateAsync(turnContext: TurnContext): Promise<void> {
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
