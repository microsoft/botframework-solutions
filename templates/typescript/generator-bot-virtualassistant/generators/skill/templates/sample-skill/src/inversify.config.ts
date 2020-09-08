/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import 'reflect-metadata';
import { decorate, injectable, Container } from 'inversify';
import { TYPES } from './types/constants';
import { IBotSettings } from './services/botSettings';
import * as appsettings from './appsettings.json';
import { ICognitiveModelConfiguration, LocaleTemplateManager } from 'bot-solutions';
import * as cognitiveModelsRaw from './cognitivemodels.json';
import {
    BotTelemetryClient,
    NullTelemetryClient,
    TelemetryLoggerMiddleware,
    UserState,
    ConversationState,
    BotFrameworkAdapterSettings,
    BotFrameworkAdapter,
    ActivityHandler,
    ActivityHandlerBase,
    StatePropertyAccessor } from 'botbuilder';
import { ApplicationInsightsTelemetryClient, TelemetryInitializerMiddleware } from 'botbuilder-applicationinsights';
import { CosmosDbPartitionedStorage } from 'botbuilder-azure';
import { join } from 'path';
import { DefaultAdapter } from './adapters';
import { BotServices } from './services/botServices';
import { Dialog, DialogContainer, ComponentDialog } from 'botbuilder-dialogs';
import { SkillDialogBase } from './dialogs/skillDialogBase';
import { SampleDialog } from './dialogs/sampleDialog';
import { SampleAction } from './dialogs/sampleAction';
import { MainDialog } from './dialogs/mainDialog';
import { DefaultActivityHandler } from './bots/defaultActivityHandler';
import { SimpleCredentialProvider } from 'botframework-connector';
import { SkillState } from './models';

const container = new Container();

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
container.bind<Partial<IBotSettings>>(TYPES.BotSettings).toConstantValue(botSettings);

// Configure configuration provider
decorate(injectable(), SimpleCredentialProvider);
container.bind<SimpleCredentialProvider>(TYPES.SimpleCredentialProvider).toConstantValue(
    new SimpleCredentialProvider(appsettings.microsoftAppId, appsettings.microsoftAppPassword)
);

// Configure telemetry
container.bind<BotTelemetryClient>(TYPES.BotTelemetryClient).toConstantValue(
    getTelemetryClient(container.get<IBotSettings>(TYPES.BotSettings))
);

decorate(injectable(), TelemetryLoggerMiddleware);
container.bind<TelemetryLoggerMiddleware>(TYPES.TelemetryLoggerMiddleware).toConstantValue(
    new TelemetryLoggerMiddleware(container.get<BotTelemetryClient>(TYPES.BotTelemetryClient))
);

decorate(injectable(), TelemetryInitializerMiddleware);
container.bind<TelemetryInitializerMiddleware>(TYPES.TelemetryInitializerMiddleware).toConstantValue(
    new TelemetryInitializerMiddleware(
        container.get<TelemetryLoggerMiddleware>(TYPES.TelemetryLoggerMiddleware)
    )
);

// Configure bot services
decorate(injectable(), BotServices);
container.bind<BotServices>(TYPES.BotServices).to(BotServices).inSingletonScope();

// Configure storage
// Uncomment the following line for local development without Cosmos Db
// decorate(injectable(), MemoryStorage);
// container.bind<Partial<MemoryStorage>>(TYPES.MemoryStorage).toConstantValue(new MemoryStorage());
decorate(injectable(), CosmosDbPartitionedStorage);
container.bind<CosmosDbPartitionedStorage>(TYPES.CosmosDbPartitionedStorage).toConstantValue(
    new CosmosDbPartitionedStorage(
        container.get<IBotSettings>(TYPES.BotSettings).cosmosDb
    )
);

decorate(injectable(), UserState);
container.bind<UserState>(TYPES.UserState).toConstantValue(
    new UserState(
        container.get<CosmosDbPartitionedStorage>(TYPES.CosmosDbPartitionedStorage)
    )
);

decorate(injectable(), ConversationState);
container.bind<ConversationState>(TYPES.ConversationState).toConstantValue(
    new ConversationState(
        container.get<CosmosDbPartitionedStorage>(TYPES.CosmosDbPartitionedStorage)
    )
);

decorate(injectable(), SkillState);
container.bind<StatePropertyAccessor<SkillState>>(TYPES.SkillState).toConstantValue(
    container.get<UserState>(TYPES.UserState).createProperty(SkillState.name)
);

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
    new LocaleTemplateManager(localizedTemplates, container.get<IBotSettings>(TYPES.BotSettings).defaultLocale || 'en-us')
);

// Register dialogs
decorate(injectable(), Dialog);
decorate(injectable(), DialogContainer);
decorate(injectable(), ComponentDialog);
decorate(injectable(), SkillDialogBase);
decorate(injectable(), SampleDialog);
decorate(injectable(), SampleAction);
decorate(injectable(), MainDialog);
container.bind<SampleDialog>(TYPES.SampleDialog).to(SampleDialog).inTransientScope();
container.bind<SampleAction>(TYPES.SampleAction).to(SampleAction).inTransientScope();
container.bind<MainDialog>(TYPES.MainDialog).to(MainDialog).inTransientScope();

// Configure adapters
const adapterSettings: Partial<BotFrameworkAdapterSettings> = {
    appId: botSettings.microsoftAppId,
    appPassword: botSettings.microsoftAppPassword
};

container.bind<Partial<BotFrameworkAdapterSettings>>(TYPES.BotFrameworkAdapterSettings).toConstantValue(
    adapterSettings
);

decorate(injectable(), DefaultAdapter);
decorate(injectable(), BotFrameworkAdapter);
container.bind<DefaultAdapter>(TYPES.DefaultAdapter).to(DefaultAdapter).inSingletonScope();

// Configure bot
decorate(injectable(), ActivityHandlerBase);
decorate(injectable(), ActivityHandler);
decorate(injectable(), DefaultActivityHandler);
container.bind<DefaultActivityHandler<MainDialog>>(TYPES.DefaultActivityHandler).to(DefaultActivityHandler);

function getTelemetryClient(settings: Partial<IBotSettings>): BotTelemetryClient {
    if (settings !== undefined && settings.appInsights !== undefined && settings.appInsights.instrumentationKey !== undefined) {
        const instrumentationKey: string = settings.appInsights.instrumentationKey;

        return new ApplicationInsightsTelemetryClient(instrumentationKey);
    }

    return new NullTelemetryClient();
}

export default container;