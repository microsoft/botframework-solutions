/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const {
  AutoSaveStateMiddleware,
  ConversationState,
  MemoryStorage,
  NullTelemetryClient,
  TelemetryLoggerMiddleware,
  UserState
} = require("botbuilder");
const {
  ApplicationInsightsTelemetryClient
} = require("botbuilder-applicationinsights");
const { TestAdapter } = require("botbuilder-core");
const {
  EventDebuggerMiddleware,
  Locales,
  ResponseManager,
  SetLocaleMiddleware
} = require("botbuilder-solutions");
const { SkillContext } = require("botbuilder-skills");
const { ActivityTypes } = require("botframework-schema");
const i18next = require("i18next");
const i18nextNodeFsBackend = require("i18next-node-fs-backend");
const SkillState = require("../../lib/models/skillState").SkillState;
const MainDialog = require("../../lib/dialogs/mainDialog.js").MainDialog;
const SampleDialog = require("../../lib/dialogs/sampleDialog.js").SampleDialog;
const DialogBot = require("../../lib/bots/dialogBot.js").DialogBot;
const MainResponses = require("../../lib/responses/main/mainResponses.js")
  .MainResponses;
const SharedResponses = require("../../lib/responses/shared/sharedResponses.js")
  .SharedResponses;
const SampleResponses = require("../../lib/responses/sample/sampleResponses.js")
  .SampleResponses;
const BotServices = require("../../lib/services/botServices.js").BotServices;
let appsettings;
let cognitiveModels = new Map();
let telemetryClient;
let userState;
let conversationState;
let cognitiveModelsRaw;
const TEST_MODE = require("../testBase").testMode;

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
      appsettings = require("../mockResources/appsettings.json");
      cognitiveModelsRaw = require("../mockResources/cognitiveModels.json");
      getCognitiveModels(cognitiveModelsRaw);
      break;
    default:
  }
};

const configuration = async function() {
  // Configure internationalization and default locale
  await i18next.use(i18nextNodeFsBackend).init({
    fallbackLng: "en",
    preload: ["de", "en", "es", "fr", "it", "zh"]
  });
  await Locales.addResourcesFromPath(i18next, "common");

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
  const skillContextAccessor = userState.createProperty(SkillContext.name);
  telemetryClient = getTelemetryClient(botSettings);
  const responseManager = new ResponseManager(
    ["en", "de", "es", "fr", "it", "zh"],
    [SampleResponses, MainResponses, SharedResponses]
  );
  const botServices = new BotServices(botSettings, telemetryClient);
  const sampleDialog = new SampleDialog(
    botSettings,
    botServices,
    responseManager,
    stateAccessor,
    telemetryClient
  );
  const mainDialog = new MainDialog(
    botSettings,
    botServices,
    responseManager,
    stateAccessor,
    skillContextAccessor,
    sampleDialog,
    telemetryClient
  );
  this.bot = new DialogBot(conversationState, telemetryClient, mainDialog);
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
  adapter.use(new AutoSaveStateMiddleware(conversationState, userState));
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

module.exports = {
  configuration: configuration,
  initialize: initialize,
  getTestAdapter: getTestAdapter
};
