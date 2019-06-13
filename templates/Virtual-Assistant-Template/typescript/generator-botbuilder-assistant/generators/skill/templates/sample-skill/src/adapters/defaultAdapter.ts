/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    AutoSaveStateMiddleware,
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ConversationState,
    ShowTypingMiddleware,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TranscriptStore,
    UserState} from 'botbuilder';
import { AzureBlobTranscriptStore } from 'botbuilder-azure';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware } from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings';

export class DefaultAdapter extends BotFrameworkAdapter {
    constructor(
        settings: Partial<IBotSettings>,
        adapterSettings: Partial<BotFrameworkAdapterSettings>,
        userState: UserState,
        conversationState: ConversationState,
        telemetryClient: BotTelemetryClient
    ) {
        super(adapterSettings);

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        const transcriptStore: TranscriptStore = new AzureBlobTranscriptStore({
            containerName: settings.blobStorage.container,
            storageAccountOrConnectionString: settings.blobStorage.connectionString
        });
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new TranscriptLoggerMiddleware(transcriptStore));
        this.use(new ShowTypingMiddleware());
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new AutoSaveStateMiddleware(conversationState, userState));
    }
}
