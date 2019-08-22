/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { writeFileSync } = require("fs");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const { TestLogger } = require("./helpers/testLogger");
const { normalizeContent } = require("./helpers/normalizeUtils");
const { AuthenticationUtils } = require("../lib/utils");
const authenticationUtils = new AuthenticationUtils();
const emptyAzureAuthSettings = JSON.stringify(require(resolve(__dirname, join("mocks", "azureAuthSettings", "emptyAuthSettings.json"))));
const filledAzureAuthSettings = JSON.stringify(require(resolve(__dirname, join("mocks", "azureAuthSettings", "filledAuthSettings.json"))));
const appShowReplyUrl = JSON.stringify(require(resolve(__dirname, join("mocks", "appShowReplyUrl", "emptyAppShowReplyUrl.json"))));

const noAuthConnectionAppsettings = normalizeContent(JSON.stringify(
    {
        "microsoftAppId": "",
        "microsoftAppPassword": "",
        "appInsights": {
            "appId": "",
            "instrumentationKey": ""
        },
        "blobStorage": {
            "connectionString": "",
            "container": ""
        },
        "cosmosDb": {
            "authkey": "",
            "collectionId": "",
            "cosmosDBEndpoint": "",
            "databaseId": ""
        },
        "contentModerator": {
            "key": ""
        }
    },
    null, 4));

const authConnectionAppsettings = normalizeContent(JSON.stringify(
    {
        "microsoftAppId": "",
        "microsoftAppPassword": "",
        "appInsights": {
            "appId": "",
            "instrumentationKey": ""
        },
        "blobStorage": {
            "connectionString": "",
            "container": ""
        },
        "cosmosDb": {
            "authkey": "",
            "collectionId": "",
            "cosmosDBEndpoint": "",
            "databaseId": ""
        },
        "contentModerator": {
            "key": ""
        },
        "oauthConnections": [
            {
                "name": "Outlook",
                "provider": "Azure Active Directory v2"
            }
        ]
    },
    null,4));

function undoChangesInTemporalFiles() {
    writeFileSync(resolve(__dirname, join("mocks", "appsettings", "noAuthConnectionAppsettings.json")), noAuthConnectionAppsettings);
    writeFileSync(resolve(__dirname, join("mocks", "appsettings", "authConnectionAppsettings.json")), authConnectionAppsettings);
}

describe("The authentication util", function() {
    beforeEach(function() {
        undoChangesInTemporalFiles();
        this.callback = sandbox.stub(authenticationUtils.childProcessUtils, "tryExecute");
    });

    afterEach(function() {
        this.callback.restore();
    });

    after(function() {
        undoChangesInTemporalFiles();
    })

    describe("should show a warning", function() {
        it("when the skill manifest doesn't contain any authentication connection", async function() {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "invalidManifest.json")),
                remoteManifest: "",
                dispatchName: "",
                language: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: new TestLogger()
            }

            await authenticationUtils.authenticate(configuration, require(configuration.localManifest), configuration.logger);

            const warningList = configuration.logger.getWarning();
            strictEqual(warningList[warningList.length - 1], `There are no authentication connections in your Skills manifest.`);
        });

        it("when the skill manifest doesn't contain an Azure Active Directory v2 as authentication connection", async function() {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "googleAuthenticationManifest.json")),
                remoteManifest: "",
                dispatchName: "",
                language: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: new TestLogger()
            }

            await authenticationUtils.authenticate(configuration, require(configuration.localManifest), configuration.logger);

            const warningList = configuration.logger.getWarning();
            strictEqual(warningList[warningList.length - 1], `For more information on setting up the authentication configuration manually go to:
https://aka.ms/vamanualauthsteps`);
            strictEqual(warningList[warningList.length - 2], `There's no Azure Active Directory v2 authentication connection in your Skills manifest. You must configure one of the following connection types MANUALLY in the Azure Portal:
        Google`);
        });

        it("when any of the external commands fails", async function() {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "azureActiveDirectoryV2AuthenticationManifest.json")),
                remoteManifest: "",
                dispatchName: "",
                language: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "noAuthConnectionAppsettings.json")),
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: new TestLogger()
            };

            sandbox.replace(authenticationUtils, "validateAzVersion", (logger) => {});
            // Mock the execution of az bot authsetting list (listAuthSettingsCommand)
            this.callback.onCall(0).returns(Promise.resolve(emptyAzureAuthSettings));
            // Mock the execution of az ad app show (azureAppShowCommand)
            this.callback.onCall(1).returns(Promise.reject(new Error("Mocked function throws an Error")));
            
            await authenticationUtils.authenticate(configuration, require(configuration.localManifest), configuration.logger);
            
            const warningList = configuration.logger.getWarning();
            strictEqual(warningList[warningList.length - 1], `For more information on setting up the authentication configuration manually go to:
https://aka.ms/vamanualauthsteps`);
            strictEqual(warningList[warningList.length - 2], `You must configure one of the following connection types MANUALLY in the Azure Portal:
        Azure Active Directory v2`);
            strictEqual(warningList[warningList.length - 3], `There was an error while executing the following command:\n\taz ad app show --id \nMocked function throws an Error`)
        });

        it("when the scopes are not configured automatically", async function() {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "azureActiveDirectoryV2AuthenticationManifest.json")),
                remoteManifest: "",
                dispatchName: "",
                language: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "authConnectionAppsettings.json")),
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: new TestLogger()
            };

            // Mock the execution of az bot authsetting list (listAuthSettingsCommand)
            this.callback.onCall(0).returns(Promise.resolve(emptyAzureAuthSettings));
            // Mock the execution of az ad app show (azureAppShowCommand)
            this.callback.onCall(1).returns(Promise.resolve(appShowReplyUrl));
            // Mock the execution of az ad app update (azureAppUpdateCommand)
            this.callback.onCall(2).returns(Promise.resolve("Mocked function throws an error"));
            
            await authenticationUtils.authenticate(configuration, require(configuration.localManifest), configuration.logger);
            
            const warningList = configuration.logger.getWarning();
            strictEqual(warningList[warningList.length - 1], `Could not configure scopes automatically.`)
        });
    });    
    
    describe("should show a message", function() {
        describe("when the authentication process finished successfully", function() {
            it("without an aadConnection", async function() {
                const configuration = {
                    botName: "",
                    localManifest: resolve(__dirname, join("mocks", "skills", "azureActiveDirectoryV2AuthenticationManifest.json")),
                    remoteManifest: "",
                    dispatchName: "",
                    language: "",
                    luisFolder: "",
                    dispatchFolder: "",
                    outFolder: "",
                    lgOutFolder: "",
                    skillsFile: "",
                    resourceGroup: "",
                    appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "authConnectionAppsettings.json")),
                    cognitiveModelsFile: "",
                    lgLanguage: "",
                    logger: new TestLogger()
                };
    
                
                // Mock the execution of az bot authsetting list (listAuthSettingsCommand)
                this.callback.onCall(0).returns(Promise.resolve(emptyAzureAuthSettings));
                // Mock the execution of az ad app show (azureAppShowCommand)
                this.callback.onCall(1).returns(Promise.resolve(appShowReplyUrl));
                // Mock the execution of az ad app update (azureAppUpdateCommand)
                this.callback.onCall(2).returns(Promise.resolve(""));
                // Mock the execution of az bot authsetting create (authSettingCommand)
                this.callback.onCall(3).returns(Promise.resolve("Mocked function successfully"));
    
                await authenticationUtils.authenticate(configuration, require(configuration.localManifest), configuration.logger);
    
                const messageList = configuration.logger.getMessage();
                strictEqual(messageList[messageList.length - 1], `Authentication process finished successfully.`);
            });

            it("with an aadConnection", async function() {
                const configuration = {
                    botName: "",
                    localManifest: resolve(__dirname, join("mocks", "skills", "azureActiveDirectoryV2AuthenticationManifest.json")),
                    remoteManifest: "",
                    dispatchName: "",
                    language: "",
                    luisFolder: "",
                    dispatchFolder: "",
                    outFolder: "",
                    lgOutFolder: "",
                    skillsFile: "",
                    resourceGroup: "",
                    appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "authConnectionAppsettings.json")),
                    cognitiveModelsFile: "",
                    lgLanguage: "",
                    logger: new TestLogger()
                };
    
                // Mock the execution of az botvalidAppsettings authsetting list (listAuthSettingsCommand)
                this.callback.onCall(0).returns(Promise.resolve(filledAzureAuthSettings));
                // Mock the execution of az bot authsetting show (showAuthSettingsCommand)
                this.callback.onCall(1).returns(Promise.resolve(JSON.stringify(JSON.parse(filledAzureAuthSettings)[0])));
                // Mock the execution of az bot authsetting delete (deleteAuthSettingCommand)
                this.callback.onCall(2).returns(Promise.resolve(""));
                // Mock the execution of az ad app show (azureAppShowCommand)
                this.callback.onCall(3).returns(Promise.resolve(appShowReplyUrl));
                // Mock the execution of az ad app update (azureAppUpdateCommand)
                this.callback.onCall(4).returns(Promise.resolve(""));
                // Mock the execution of az bot authsetting create (authSettingCommand)
                this.callback.onCall(5).returns(Promise.resolve("Mocked function successfully"));
    
                await authenticationUtils.authenticate(configuration, require(configuration.localManifest), configuration.logger);
    
                const messageList = configuration.logger.getMessage();
                strictEqual(messageList[messageList.length - 1], `Authentication process finished successfully.`);
            });
        });
    });
});