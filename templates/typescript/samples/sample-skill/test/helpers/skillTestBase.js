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
const {
    ApplicationInsightsTelemetryClient
} = require("botbuilder-applicationinsights");
const { TestAdapter } = require("botbuilder-core");
const {
    EventDebuggerMiddleware,
    Locales,
    SetLocaleMiddleware,
    LocaleTemplateEngineManager,
} = require("botbuilder-solutions");
const { ActivityTypes } = require("botframework-schema");
const i18next = require("i18next");
const i18nextNodeFsBackend = require("i18next-node-fs-backend");
const SkillState = require("../../lib/models/skillState").SkillState;
const DefaultActivityHandler = require("../../lib/bots/defaultActivityHandler.js").DefaultActivityHandler;
const MainDialog = require("../../lib/dialogs/mainDialog.js").MainDialog;
const SampleDialog = require("../../lib/dialogs/sampleDialog.js").SampleDialog;
const BotServices = require("../../lib/services/botServices.js").BotServices;
let appsettings;
let cognitiveModels = new Map();
let telemetryClient;
let userState;
let conversationState;
let cognitiveModelsRaw;
const TEST_MODE = require("./testBase").testMode;
const localizedTemplates = new Map();
const templateFiles = ['MainResponses','SampleResponses'];
const supportedLocales =  ['en-us','de-de','es-es','fr-fr','it-it','zh-cn'];
supportedLocales.forEach(locale => {
    const localeTemplateFiles = [];
    templateFiles.forEach(template => {
        // LG template for default locale should not include locale in file extension.
        if (locale === 'en-us'){
            localeTemplateFiles.push(join(__dirname, '..', '..', 'src', 'responses', `${template}.lg`));
        }
        else {
            localeTemplateFiles.push(join(__dirname, '..', '..', 'src', 'responses', `${template}.${locale}.lg`));
        }
    });

    localizedTemplates.set(locale, localeTemplateFiles);
});

const templateEngine = new LocaleTemplateEngineManager(localizedTemplates, 'en-us');


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
    // Configure internationalization and default locale
    await i18next.use(i18nextNodeFsBackend).init({
        fallbackLng: 'en-us',
        preload: ['de-de', 'en-us', 'es-es', 'fr-fr', 'it-it', 'zh-cn'],
    });
    await Locales.addResourcesFromPath(i18next, "common");
    await i18next.changeLanguage('en-us');

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
        telemetryClient,
        templateEngine
    );
    const mainDialog = new MainDialog(
        botServices,
        stateAccessor,
        sampleDialog,
        telemetryClient,
        templateEngine,
    );
    this.bot = new DefaultActivityHandler(conversationState, userState, mainDialog);
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

const getTemplates = function(name) {
    return templateEngine.templateEnginesPerLocale.get(i18next.language).expandTemplate(name);
};

module.exports = {
    configuration: configuration,
    initialize: initialize,
    getTestAdapter: getTestAdapter,
    templateEngine: templateEngine,
    getTemplates: getTemplates
};
