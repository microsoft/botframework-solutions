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
    UserState } from 'botbuilder';
import {
    CosmosDbStorage,
    CosmosDbStorageSettings } from 'botbuilder-azure';
import { ISkillManifest } from 'botbuilder-skills';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware,
    TelemetryLoggerMiddleware} from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings.js';

export class DefaultAdapter extends BotFrameworkAdapter {
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

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        if (settings.appInsights === undefined) {
            throw new Error('There is no appInsights value in appsettings file');
        }
        this.telemetryClient = new TelemetryClient(settings.appInsights.instrumentationKey);

        this.use(new TelemetryLoggerMiddleware(this.telemetryClient, true));
        // Currently not working https://github.com/Microsoft/botbuilder-js/issues/853#issuecomment-481416004
        // this.use(new TranscriptLoggerMiddleware(this.transcriptStore));
        // Typing Middleware (automatically shows typing when the bot is responding/working)
        this.use(new ShowTypingMiddleware());
        let defaultLocale: string = 'en-us';
        if (settings.defaultLocale !== undefined) {
            defaultLocale = settings.defaultLocale;
        }
        this.use(new SetLocaleMiddleware(defaultLocale));
        this.use(new EventDebuggerMiddleware());
        // Use the AutoSaveStateMiddleware middleware to automatically read and write conversation and user state.
        this.use(new AutoSaveStateMiddleware(this.conversationState, this.userState));
    }
}
