const { join } = require("path");
const restify = require("restify");
const { manifestGenerator } = require("../../lib/skillManifestGenerator");

const manifestFile = join(__dirname, '..', 'mocks', 'resources', 'manifestTemplate.json');
const microsoftAppId = '9afc4045-b3f3-4106-80be-d152d8821879';
const microsoftAppPassword = 'MockPassword';
const languageModels = [
    {
        id: "Calendar",
        name: "Calendar",
        region: "westus",
        version: "0.1",
        authoringKey: "AUTHORINGKEY",
        subscriptionKey: "SUBSCRIPTIONKEY"
    }
];
const cognitiveModels = new Map();
cognitiveModels.set('en', {
    languageModels: languageModels
});
cognitiveModels.set('de', {
    languageModels: languageModels
});
cognitiveModels.set('fr', {
    languageModels: languageModels
});
const botSettings = {
    microsoftAppId: microsoftAppId,
    microsoftAppPassword: microsoftAppPassword,
    cognitiveModels: cognitiveModels,
};
const server = restify.createServer();
server.use(restify.plugins.queryParser());
server.use(restify.plugins.bodyParser());

server.get('/api/manifest', manifestGenerator(manifestFile, botSettings));

exports.server = server;
exports.botSettings = botSettings;
