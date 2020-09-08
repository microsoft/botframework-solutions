/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ActivityTypes,
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ShowTypingMiddleware,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TurnContext } from 'botbuilder';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware,
    LocaleTemplateManager,
    SetSpeakMiddleware } from 'bot-solutions';
import { IBotSettings } from '../services/botSettings';
import { TurnContextEx } from '../extensions/turnContextEx';
import { AzureBlobTranscriptStore, BlobStorageSettings } from 'botbuilder-azure';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { Activity } from 'botframework-schema';
import { inject } from 'inversify';
import { TYPES } from '../types/constants';

export class DefaultAdapter extends BotFrameworkAdapter {

    public constructor(@inject(TYPES.BotSettings) settings: Partial<IBotSettings>,
        @inject(TYPES.BotFrameworkAdapterSettings) adapterSettings: Partial<BotFrameworkAdapterSettings>,
        @inject(TYPES.LocaleTemplateManager) templateManager: LocaleTemplateManager,
        @inject(TYPES.TelemetryInitializerMiddleware) telemetryMiddleware: TelemetryInitializerMiddleware,
        @inject(TYPES.BotTelemetryClient) telemetryClient: BotTelemetryClient,
    ) {
        super(adapterSettings);
        this.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.message || JSON.stringify(error)
            });

            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.stack
            });

            await context.sendActivity(templateManager.generateActivityForLocale('ErrorMessage', context.activity.locale));
            telemetryClient.trackException({ exception: error });

            if (TurnContextEx.isSkill(context)){
                // Send and EndOfConversation activity to the skill caller with the error to end the conversation
                // and let the caller decide what to do.
                const endOfconversation: Partial<Activity> = {
                    type: ActivityTypes.EndOfConversation,
                    code: 'SkillError',
                    text: error.message
                };

                await context.sendActivity(endOfconversation);
            }
        };

        this.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.message || JSON.stringify(error)
            });

            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.stack
            });

            await context.sendActivity(templateManager.generateActivityForLocale('ErrorMessage', context.activity.locale));
            telemetryClient.trackException({ exception: error });
        };
        
        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        this.use(telemetryMiddleware);
        
        // Uncomment the following line for local development without Azure Storage
        // this.use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
        const blobStorageSettings: BlobStorageSettings = { containerName: settings.blobStorage.container, storageAccountOrConnectionString: settings.blobStorage.connectionString};
        this.use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(blobStorageSettings)));
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new ShowTypingMiddleware());
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new SetSpeakMiddleware());
    }
}
