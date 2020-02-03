/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ConversationState,
    ShowTypingMiddleware,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TranscriptStore } from 'botbuilder';
import { AzureBlobTranscriptStore } from 'botbuilder-azure';
import { WhitelistAuthenticationProvider } from 'botbuilder-skills';
import {
    EventDebuggerMiddleware,
    FeedbackMiddleware,
    SetLocaleMiddleware } from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings';

export class DefaultAdapter extends BotFrameworkAdapter {
    private whitelistAuthenticationProvider: WhitelistAuthenticationProvider;

    public constructor(
        settings: Partial<IBotSettings>,
        adapterSettings: Partial<BotFrameworkAdapterSettings>,
        conversationState: ConversationState,
        telemetryClient: BotTelemetryClient,
        whitelistAuthenticationProvider: WhitelistAuthenticationProvider
    ) {
        super(adapterSettings);

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        const transcriptStore: TranscriptStore = new AzureBlobTranscriptStore({
            containerName: settings.blobStorage.container,
            storageAccountOrConnectionString: settings.blobStorage.connectionString
        });

        this.whitelistAuthenticationProvider = whitelistAuthenticationProvider;

        // Uncomment the following line for local development without Azure Storage
        // this.use(new TranscriptLoggerMiddleware(new MemoryTranscriptStore()));
        this.use(new TranscriptLoggerMiddleware(transcriptStore));
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new ShowTypingMiddleware());
        this.use(new FeedbackMiddleware(conversationState, telemetryClient));
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
    }
}
