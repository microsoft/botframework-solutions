/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotTelemetryClient,
    ConversationState,
    StatePropertyAccessor,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TranscriptStore } from 'botbuilder';
import { AzureBlobTranscriptStore } from 'botbuilder-azure';
import { DialogState } from 'botbuilder-dialogs';
import {
    SkillHttpBotAdapter,
    SkillMiddleware } from 'botbuilder-skills';
import { EventDebuggerMiddleware, SetLocaleMiddleware } from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings';

export class CustomSkillAdapter extends SkillHttpBotAdapter {

    public constructor(
        settings: Partial<IBotSettings>,
        conversationState: ConversationState,
        telemetryClient: BotTelemetryClient,
        dialogStateAccessor: StatePropertyAccessor<DialogState>
    ) {
        super(telemetryClient);

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        const transcriptStore: TranscriptStore = new AzureBlobTranscriptStore({
            containerName: settings.blobStorage.container,
            storageAccountOrConnectionString: settings.blobStorage.connectionString
        });

        // Uncomment the following line for local development without Azure Storage
        // this.use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
        this.use(new TranscriptLoggerMiddleware(transcriptStore));
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new SkillMiddleware(conversationState, dialogStateAccessor));
    }
}
