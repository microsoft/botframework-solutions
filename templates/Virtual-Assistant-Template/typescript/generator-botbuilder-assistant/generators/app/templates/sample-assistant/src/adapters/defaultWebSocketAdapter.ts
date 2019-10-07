/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotTelemetryClient,
    ShowTypingMiddleware,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TranscriptStore } from 'botbuilder';
import { AzureBlobTranscriptStore } from 'botbuilder-azure';
import { Dialog } from 'botbuilder-dialogs';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware,
    SetSpeakMiddleware } from 'botbuilder-solutions';
import { BotFrameworkStreamingAdapter } from 'botbuilder-streaming-extensions';
import { DialogBot } from '../bots/dialogBot';
import { IBotSettings } from '../services/botSettings';

export class DefaultWebSocketAdapter extends BotFrameworkStreamingAdapter {

    public constructor(
        bot: DialogBot<Dialog>,
        settings: Partial<IBotSettings>,
        telemetryClient: BotTelemetryClient) {
        super(bot);

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
        this.use(new ShowTypingMiddleware());
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new SetSpeakMiddleware(settings.defaultLocale || 'en-us'));
    }
}
