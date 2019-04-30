/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TelemetryClient } from 'applicationinsights';
import {
    AutoSaveStateMiddleware,
    ConversationState,
    ShowTypingMiddleware,
    UserState } from 'botbuilder';
import {
    CosmosDbStorageSettings } from 'botbuilder-azure';
import { SkillAdapter } from 'botbuilder-skills';
import {
    EventDebuggerMiddleware,
    SetLocaleMiddleware,
    TelemetryLoggerMiddleware} from 'botbuilder-solutions';
import { IBotSettings } from '../services/botSettings';

export class CustomSkillAdapter extends SkillAdapter {
    private readonly solutionName: string = '<%=skillName%>';
    public readonly cosmosDbStorageSettings: CosmosDbStorageSettings;
    public readonly telemetryClient: TelemetryClient;

    constructor(
        settings: Partial<IBotSettings>,
        userState: UserState,
        conversationState: ConversationState
    ) {
        super();

        if (settings.cosmosDb === undefined) {
            throw new Error('There is no cosmosDb value in appsettings file');
        }

        this.cosmosDbStorageSettings = {
            authKey: settings.cosmosDb.authkey,
            collectionId: settings.cosmosDb.collectionId,
            databaseId: settings.cosmosDb.databaseId,
            serviceEndpoint: settings.cosmosDb.cosmosDBEndpoint
        };

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
        this.use(new AutoSaveStateMiddleware(conversationState, userState));
        // PENDING
        // this.use(new SkillMiddleware(userState, conversationState, conversationState.createProperty(this.solutionName)))
    }
}
