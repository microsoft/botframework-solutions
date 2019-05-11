/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ActivityTypes,
    AutoSaveStateMiddleware,
    BotFrameworkAdapter,
    BotFrameworkAdapterSettings,
    BotTelemetryClient,
    ConversationState,
    ShowTypingMiddleware,
    TelemetryLoggerMiddleware,
    TranscriptLoggerMiddleware,
    TranscriptStore,
    TurnContext,
    UserState
} from 'botbuilder';
import {
    AzureBlobTranscriptStore,
    CosmosDbStorage,
    CosmosDbStorageSettings
} from 'botbuilder-azure';
import { ISkillManifest } from 'botbuilder-skills';
import { EventDebuggerMiddleware, SetLocaleMiddleware } from 'botbuilder-solutions';
import i18next from 'i18next';
import { IBotSettings } from '../services/botSettings.js';

export class DefaultAdapter extends BotFrameworkAdapter {
    public readonly conversationState: ConversationState;
    public readonly skills: ISkillManifest[] = [];
    public readonly userState: UserState;

    constructor(
        settings: Partial<IBotSettings>,
        adapterSettings: Partial<BotFrameworkAdapterSettings>,
        telemetryClient: BotTelemetryClient
    ) {
        super(adapterSettings);

        this.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.message
            });
            await context.sendActivity({
                type: ActivityTypes.Trace,
                text: error.stack
            });
            await context.sendActivity(i18next.t('main.error'));
            telemetryClient.trackException({ exception: error });
        };

        if (settings.cosmosDb === undefined) {
            throw new Error('There is no cosmosDb value in appsettings file');
        }
        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        if (settings.appInsights === undefined) {
            throw new Error('There is no appInsights value in appsettings file');
        }

        const cosmosDbStorageSettings: CosmosDbStorageSettings = {
            authKey: settings.cosmosDb.authkey,
            collectionId: settings.cosmosDb.collectionId,
            databaseId: settings.cosmosDb.databaseId,
            serviceEndpoint: settings.cosmosDb.cosmosDBEndpoint
        };

        const storage: CosmosDbStorage = new CosmosDbStorage(cosmosDbStorageSettings);
        // create conversation and user state
        this.conversationState = new ConversationState(storage);
        this.userState = new UserState(storage);

        if (settings.blobStorage === undefined) {
            throw new Error('There is no blobStorage value in appsettings file');
        }

        const transcriptStore: TranscriptStore = new AzureBlobTranscriptStore({
            containerName: settings.blobStorage.container,
            storageAccountOrConnectionString: settings.blobStorage.connectionString
        });

        this.use(new TranscriptLoggerMiddleware(transcriptStore));
        this.use(new TelemetryLoggerMiddleware(telemetryClient, true));
        this.use(new ShowTypingMiddleware());
        this.use(new SetLocaleMiddleware(settings.defaultLocale || 'en-us'));
        this.use(new EventDebuggerMiddleware());
        this.use(new AutoSaveStateMiddleware(this.conversationState, this.userState));
    }
}
