/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const {
    ConversationState,
    MemoryStorage,
    NullTelemetryClient,
    TelemetryLoggerMiddleware,
    UserState
} = require("botbuilder");
const { join } = require("path");
const { TemplatesParser } = require("botbuilder-lg");
const {
    ApplicationInsightsTelemetryClient
} = require("botbuilder-applicationinsights");
const { TestAdapter } = require("botbuilder-core");
const {
    EventDebuggerMiddleware,
    Locales,
    SetLocaleMiddleware,
    LocaleTemplateManager,
} = require("bot-solutions");
const { ActivityTypes } = require("botframework-schema");
const SkillState = require("../../lib/models/skillState").SkillState;
const DefaultActivityHandler = require("../../lib/bots/defaultActivityHandler.js").DefaultActivityHandler;
const MainDialog = require("../../lib/dialogs/mainDialog.js").MainDialog;
const SampleDialog = require("../../lib/dialogs/sampleDialog.js").SampleDialog;
const SampleAction = require("../../lib/dialogs/sampleAction.js").SampleAction;
const BotServices = require("../../lib/services/botServices.js").BotServices;
let appsettings;
let cognitiveModels = new Map();
let telemetryClient;
let userState;
let conversationState;
let cognitiveModelsRaw;
const TEST_MODE = require("./testBase").testMode;
const localizedTemplates = new Map();
const templateFile = 'AllResponses';
const supportedLocales = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];

supportedLocales.forEach((locale) => {
    // LG template for en-us does not include locale in file extension.
    const localTemplateFile = locale === 'en-us'
        ? join(__dirname, '..', '..', 'lib', 'responses', `${templateFile}.lg`)
        : join(__dirname, '..', '..', 'lib', 'responses', `${templateFile}.${locale}.lg`)
    localizedTemplates.set(locale, localTemplateFile);
});

const templateManager = new LocaleTemplateManager(localizedTemplates);

const getCognitiveModels = function(cognitiveModelsRaw) {
    const cognitiveModelDictionary = cognitiveModelsRaw.cognitiveModels;
    const cognitiveModelMap = new Map(Object.entries(cognitiveModelDictionary));
    cognitiveModelMap.forEach((value, key) => {
        cognitiveModels.set(key, value);
    });
};

const setupEnvironment = function(testMode) {
    switch (testMode) {
        case "record":
            appsettings = require("../../src/appsettings.json");
            cognitiveModelsRaw = require("../../src/cognitiveModels.json");
            getCognitiveModels(cognitiveModelsRaw);
            break;
        case "lockdown":
            appsettings = require("../mocks/resources/appsettings.json");
            cognitiveModelsRaw = require("../mocks/resources/cognitiveModels.json");
            getCognitiveModels(cognitiveModelsRaw);
            break;
        default:
    }
};

const configuration = async function() {
    setupEnvironment(TEST_MODE);
};

/**
 * Initializes the properties for the bot to be tested.
 */
const initialize = async function() {
    await configuration();
    const botSettings = {
        appInsights: appsettings.appInsights,
        blobStorage: appsettings.blobStorage,
        cognitiveModels: cognitiveModels,
        cosmosDb: appsettings.cosmosDb,
        defaultLocale: cognitiveModelsRaw.defaultLocale,
        microsoftAppId: appsettings.microsoftAppId,
        microsoftAppPassword: appsettings.microsoftAppPassword
    };
    const storage = new MemoryStorage();
    userState = new UserState(storage);
    conversationState = new ConversationState(storage);
    const stateAccessor = userState.createProperty(SkillState.name);
    telemetryClient = getTelemetryClient(botSettings);
   
    const botServices = new BotServices(botSettings, telemetryClient);
    const sampleDialog = new SampleDialog(
        botSettings,
        botServices,
        stateAccessor,
        templateManager
    );
    const sampleAction = new SampleAction(
        botSettings,
        botServices,
        stateAccessor,
        templateManager
    );
    const mainDialog = new MainDialog(
        botServices,
        sampleDialog,
        sampleAction,
        templateManager
    );
    this.bot = new DefaultActivityHandler(conversationState, userState, templateManager, telemetryClient, mainDialog);
};

/**
 * Initializes the TestAdapter.
 * @returns {TestAdapter} with the Bot logic configured.
 */
const getTestAdapter = function() {
    const bot = this.bot;

    const adapter = new TestAdapter(bot.run.bind(bot));

    adapter.onTurnError = async function(context, error) {
        await context.sendActivity({
            type: ActivityTypes.Trace,
            text: error.message
        });
        await context.sendActivity({
            type: ActivityTypes.Trace,
            text: error.stack
        });
        telemetryClient.trackException({ exception: error });
    };

    adapter.use(new TelemetryLoggerMiddleware(telemetryClient, true));
    adapter.use(
        new SetLocaleMiddleware(cognitiveModelsRaw.defaultLocale || "en-us")
    );
    adapter.use(new EventDebuggerMiddleware());
    return adapter;
};

const getTelemetryClient = function(settings) {
    if (
        settings &&
      settings.appInsights &&
      settings.appInsights.instrumentationKey
    ) {
        const instrumentationKey = settings.appInsights.instrumentationKey;

        return new ApplicationInsightsTelemetryClient(instrumentationKey);
    }

    return new NullTelemetryClient();
};

const getTemplates = function(locale, name, data) {
    const path = locale === 'en-us'
        ? join(__dirname, '..', '..', 'lib', 'responses', 'AllResponses.lg')
        : join(__dirname, '..', '..', 'lib', 'responses', `AllResponses.${locale}.lg`)
    
    return TemplatesParser.parseFile(path).expandTemplate(name, data);
};

module.exports = {
    configuration: configuration,
    initialize: initialize,
    getTestAdapter: getTestAdapter,
    templateManager: templateManager,
    getTemplates: getTemplates
};
