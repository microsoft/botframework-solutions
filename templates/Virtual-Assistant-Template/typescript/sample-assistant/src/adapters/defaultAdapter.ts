/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TelemetryClient } from 'applicationinsights';
import {
    AutoSaveStateMiddleware,
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    ConversationState,
    ShowTypingMiddleware,
    TranscriptLoggerMiddleware,
    UserState
} from 'botbuilder';
import {
    AzureBlobTranscriptStore,
    CosmosDbStorage,
    CosmosDbStorageSettings
} from 'botbuilder-azure';
import { ISkillManifest } from 'botbuilder-skills';
import {
    EventDebuggerMiddleware,
    ProactiveState,
    ProactiveStateMiddleware,
    SetLocaleMiddleware } from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings.js';

export class DefaultAdapter extends BotFrameworkAdapter {
    private readonly transcriptStore: AzureBlobTranscriptStore;
    private readonly proactiveState: ProactiveState;
    public readonly conversationState: ConversationState;
    public readonly cosmosDbStorageSettings: CosmosDbStorageSettings;
    public readonly skills: ISkillManifest[] = [];
    public readonly telemetryClient: TelemetryClient;
    public readonly userState: UserState;

    constructor(
        settings: Partial<IBotSettings>,
        adapterSettings: Partial<BotFrameworkAdapterSettings>
    ) {
        super(adapterSettings);

        if (settings.cosmosDb === undefined) {
            throw new Error('There is no cosmosDb value in appsettings file');
        }

        this.cosmosDbStorageSettings = {
            authKey: settings.cosmosDb.authkey,
            collectionId: settings.cosmosDb.collectionId,
            databaseId: settings.cosmosDb.databaseId,
            serviceEndpoint: settings.cosmosDb.cosmosDBEndpoint
        };

        const storage: CosmosDbStorage = new CosmosDbStorage(this.cosmosDbStorageSettings);

        // create conversation and user state
        this.conversationState = new ConversationState(storage);
        this.userState = new UserState(storage);
        this.proactiveState = new ProactiveState(storage);

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        this.transcriptStore = new AzureBlobTranscriptStore({
            containerName: settings.blobStorage.container,
            storageAccountOrConnectionString: settings.blobStorage.connectionString
        });

        if (settings.appInsights === undefined) {
            throw new Error('There is no appInsights value in appsettings file');
        }
        this.telemetryClient = new TelemetryClient(settings.appInsights.instrumentationKey);

        // Use the AutoSaveStateMiddleware middleware to automatically read and write conversation and user state.
        this.use(new AutoSaveStateMiddleware(this.conversationState, this.userState));
        // Currently not working https://github.com/Microsoft/botbuilder-js/issues/853#issuecomment-481416004
        // this.use(new TranscriptLoggerMiddleware(this.transcriptStore));

        // Typing Middleware (automatically shows typing when the bot is responding/working)
        this.use(new ShowTypingMiddleware());
        if (settings.defaultLocale === undefined) {
            throw new Error('There is no defaultLocale value in appsettings file');
        }
        this.use(new SetLocaleMiddleware(settings.defaultLocale));
        this.use(new EventDebuggerMiddleware());
        this.use(new ProactiveStateMiddleware(this.proactiveState));
    }
}
