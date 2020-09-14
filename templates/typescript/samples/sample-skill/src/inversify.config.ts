/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import 'reflect-metadata';
import { decorate, injectable, Container, inject } from 'inversify';
import { TYPES } from './types/constants';
import { IBotSettings } from './services/botSettings';
import * as appsettings from './appsettings.json';
import { ICognitiveModelConfiguration, LocaleTemplateManager } from 'bot-solutions';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import { BotTelemetryClient, NullTelemetryClient, TelemetryLoggerMiddleware, UserState, ConversationState, BotFrameworkAdapterSettings } from 'botbuilder';
import { ApplicationInsightsTelemetryClient, TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbPartitionedStorage } from 'botbuilder-azure';
import { join } from 'path';
import { DefaultAdapter } from './adapters';
import { BotServices } from './services/botServices';
import { SampleDialog } from './dialogs/sampleDialog';
import { SampleAction } from './dialogs/sampleAction';
import { MainDialog } from './dialogs/mainDialog';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';
import { SimpleCredentialProvider } from 'botframework-connector';

const container = new Container({skipBaseClassChecks: true});

const cognitiveModels: Map<string, ICognitiveModelConfiguration> = new Map();
const cognitiveModelDictionary: { [key: string]: Object } = cognitiveModelsRaw.cognitiveModels;
const cognitiveModelMap: Map<string, Object> = new Map(Object.entries(cognitiveModelDictionary));
cognitiveModelMap.forEach((value: Object, key: string): void => {
    cognitiveModels.set(key, value as ICognitiveModelConfiguration);
});

const botSettings: Partial<IBotSettings> = {
    appInsights: appsettings.appInsights,
    blobStorage: appsettings.blobStorage,
    cognitiveModels: cognitiveModels,
    cosmosDb: appsettings.cosmosDb,
    defaultLocale: cognitiveModelsRaw.defaultLocale,
    microsoftAppId: appsettings.microsoftAppId,
    microsoftAppPassword: appsettings.microsoftAppPassword
};

// Load settings
container.bind(TYPES.MicrosoftAppId).toConstantValue(appsettings.microsoftAppId);
container.bind(TYPES.MicrosoftAppPassword).toConstantValue(appsettings.microsoftAppPassword);
container.bind<Partial<IBotSettings>>(TYPES.BotSettings).toConstantValue(botSettings);

// Configure configuration provider
decorate(injectable(), SimpleCredentialProvider);
decorate(inject(TYPES.MicrosoftAppId), SimpleCredentialProvider, 0);
decorate(inject(TYPES.MicrosoftAppPassword), SimpleCredentialProvider, 1);
container.bind<SimpleCredentialProvider>(TYPES.SimpleCredentialProvider).to(SimpleCredentialProvider).inSingletonScope();

// Configure telemetry
container.bind<BotTelemetryClient>(TYPES.BotTelemetryClient).toConstantValue(
    getTelemetryClient(container.get<IBotSettings>(TYPES.BotSettings))
);

decorate(injectable(), TelemetryLoggerMiddleware);
decorate(inject(TYPES.BotTelemetryClient), TelemetryLoggerMiddleware, 0);
container.bind<TelemetryLoggerMiddleware>(TYPES.TelemetryLoggerMiddleware).to(TelemetryLoggerMiddleware).inSingletonScope();

decorate(injectable(), TelemetryInitializerMiddleware);
decorate(inject(TYPES.TelemetryLoggerMiddleware), TelemetryInitializerMiddleware, 0);
container.bind<TelemetryInitializerMiddleware>(TYPES.TelemetryInitializerMiddleware).to(TelemetryInitializerMiddleware).inSingletonScope();

// Configure bot services
container.bind<BotServices>(TYPES.BotServices).to(BotServices).inSingletonScope();

// Configure storage
// Uncomment the following line for local development without Cosmos Db
// decorate(injectable(), MemoryStorage);
// container.bind<Partial<MemoryStorage>>(TYPES.MemoryStorage).to(MemoryStorage).inSingletonScope();
decorate(injectable(), CosmosDbPartitionedStorage);
container.bind<CosmosDbPartitionedStorage>(TYPES.CosmosDbPartitionedStorage).toConstantValue(
    new CosmosDbPartitionedStorage(botSettings.cosmosDb)
);

decorate(injectable(), UserState);
decorate(inject(TYPES.CosmosDbPartitionedStorage), UserState, 0);
container.bind<UserState>(TYPES.UserState).to(UserState).inSingletonScope();

decorate(injectable(), ConversationState);
decorate(inject(TYPES.CosmosDbPartitionedStorage), ConversationState, 0);
container.bind<ConversationState>(TYPES.ConversationState).to(ConversationState).inSingletonScope();

// Configure localized responses
const supportedLocales: string[] = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];
const localizedTemplates: Map<string, string> = new Map<string, string>();
const templateFile = 'AllResponses';
supportedLocales.forEach((locale: string) => {
    // LG template for en-us does not include locale in file extension.
    const localTemplateFile = locale === 'en-us'
        ? join(__dirname, 'responses', `${ templateFile }.lg`)
        : join(__dirname, 'responses', `${ templateFile }.${ locale }.lg`);
    localizedTemplates.set(locale, localTemplateFile);
});

decorate(injectable(), LocaleTemplateManager);
container.bind<LocaleTemplateManager>(TYPES.LocaleTemplateManager).toConstantValue(
    new LocaleTemplateManager(localizedTemplates, botSettings.defaultLocale || 'en-us')
);

// Register dialogs
container.bind<SampleDialog>(TYPES.SampleDialog).to(SampleDialog).inTransientScope();
container.bind<SampleAction>(TYPES.SampleAction).to(SampleAction).inTransientScope();
container.bind<MainDialog>(TYPES.MainDialog).to(MainDialog).inTransientScope();

// Configure adapters
const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};

container.bind<Partial<BotFrameworkAdapterSettings>>(TYPES.BotFrameworkAdapterSettings).toConstantValue(adapterSettings);
container.bind<DefaultAdapter>(TYPES.DefaultAdapter).to(DefaultAdapter).inSingletonScope();

// Configure bot
container.bind<DefaultActivityHandler<MainDialog>>(TYPES.DefaultActivityHandler).to(DefaultActivityHandler);

function getTelemetryClient(settings: Partial<IBotSettings>): BotTelemetryClient {
    if (settings !== undefined && settings.appInsights !== undefined && settings.appInsights.instrumentationKey !== undefined) {
        const instrumentationKey: string = settings.appInsights.instrumentationKey;

        return new ApplicationInsightsTelemetryClient(instrumentationKey);
    }

    return new NullTelemetryClient();
}

export default container;