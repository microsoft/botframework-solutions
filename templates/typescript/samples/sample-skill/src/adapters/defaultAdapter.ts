/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ActivityTypes,
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ConversationState,
    ShowTypingMiddleware,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    UserState,
    TurnContext } from 'botbuilder';
import { DialogState } from 'botbuilder-dialogs';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware,
    LocaleTemplateEngineManager,
    SetSpeakMiddleware,
    SkillMiddleware } from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings';
import { AzureBlobTranscriptStore } from 'botbuilder-azure';
import { TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { Activity } from 'botbuilder-schema';

export class DefaultAdapter extends BotFrameworkAdapter {
    public constructor(
        settings: Partial<IBotSettings>,
        userState: UserState,
        conversationState: ConversationState,
        adapterSettings: Partial<BotFrameworkAdapterSettings>,
        templateEngine: LocaleTemplateEngineManager,
        telemetryMiddleware: TelemetryInitializerMiddleware,
        telemetryClient: BotTelemetryClient,
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
            
            await context.sendActivity(templateEngine.generateActivityForLocale('ErrorMessage'));
            telemetryClient.trackException({ exception: error });

            if (context.isSkill()){
                // Send and EndOfConversation activity to the skill caller with the error to end the conversation
                // and let the caller decide what to do.
                const endOfconversation = new Activity(ActivityTypes.EndOfConversation)
                endOfconversation.code = "SkillError";
                endOfconversation.text = error.message;
                await context.sendActivity(endOfconversation);
            }
        };
        
        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        this.use(telemetryMiddleware)
        
        // Uncomment the following line for local development without Azure Storage
        // this.use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
        this.use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.blobStorage.connectionString, settings.blobStorage.container)));
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new ShowTypingMiddleware());
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new SkillMiddleware(userState, conversationState, conversationState.createProperty(DialogState.name)));
        this.use(new SetSpeakMiddleware());
    }
}
