/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ActivityTypes,
    BotTelemetryClient,
    ConversationState,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TranscriptStore,
    TurnContext,
    UserState } from 'botbuilder';
import { AzureBlobTranscriptStore } from 'botbuilder-azure';
import {
    EventDebuggerMiddleware,
    LocaleTemplateEngineManager,
    SetLocaleMiddleware,
    SkillHttpBotAdapter,
    SkillMiddleware } from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';

export class CustomSkillAdapter extends SkillHttpBotAdapter {

    public constructor(
        settings: Partial<IBotSettings>,
        userState: UserState,
        conversationState: ConversationState,
        templateEngine: LocaleTemplateEngineManager,
        telemetryMiddleware: TelemetryInitializerMiddleware,
        telemetryClient: BotTelemetryClient) {
        super(telemetryClient);

        this.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.message || JSON.stringify(error)
            });
            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.stack
            });
            await context.sendActivity(templateEngine.generateActivityForLocale('ErrorMessage'));
            telemetryClient.trackException({ exception: error });
        };

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        const transcriptStore: TranscriptStore = new AzureBlobTranscriptStore({
            containerName: settings.blobStorage.container,
            storageAccountOrConnectionString: settings.blobStorage.connectionString
        });

        this.use(telemetryMiddleware);

        // Uncomment the following line for local development without Azure Storage
        // this.use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
        this.use(new TranscriptLoggerMiddleware(transcriptStore));
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new SkillMiddleware(userState, conversationState, conversationState.createProperty('DialogState')));
    }
}
