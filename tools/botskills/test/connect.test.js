/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { writeFileSync, readFileSync } = require("fs");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");
const { getNormalizedFile } = require("./helpers/normalizeUtils");
const botskills = require("../lib/index");
const emptyAppsettings = getNormalizedFile(resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")));
const appsettingsWithTestSkill = getNormalizedFile(resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")));
const authConnectionAppsettings = getNormalizedFile(resolve(__dirname, join("mocks", "appsettings", "authConnectionAppsettings.json")));
const { EOL } = require('os');

function undoChangesInTemporalFiles() {
    writeFileSync(resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")), emptyAppsettings);
    writeFileSync(resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")), appsettingsWithTestSkill);
    writeFileSync(resolve(__dirname, join("mocks", "appsettings", "authConnectionAppsettings.json")), authConnectionAppsettings);
}

describe("The connect command", function () {
    beforeEach(function() {
        undoChangesInTemporalFiles();
        this.logger = new testLogger.TestLogger();
        this.connector = new botskills.ConnectSkill();
        this.connector.logger = this.logger;
    });

    after(function() {
        undoChangesInTemporalFiles();
    });

	describe("should show an error", function () {
        it("when there is no skills File", async function () {
            const configuration = {
                botName: "",
                localManifest: "",
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: Either the 'localManifest' or 'remoteManifest' argument should be passed.`);
        });

        it("when there is no cognitiveModels file", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "nonCognitiveModels.json"),
                lgLanguage: "",
                logger: this.logger
            };
            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();
            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: Could not find the cognitiveModels file (${configuration.cognitiveModelsFile}). Please provide the '--cognitiveModelsFile' argument.`);
        });

        it("when the localManifest points to a nonexisting Skill manifest file", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "nonexistentSkill.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: The 'localManifest' argument leads to a non-existing file.${
                EOL }Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        });

        it("when the remoteManifest points to a nonexisting Skill manifest URL", async function() {
            const configuration = {
                botName: "",
                localManifest: "",
                remoteManifest: "http://nonexistentSkill.azurewebsites.net/api/skill/manifest",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1].includes('getaddrinfo ENOTFOUND'), true);
        });

        it("when the luisFolder leads to a nonexistent folder", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "fail", "nonexistentLuis")),
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: Path to the LUIS folder (${configuration.luisFolder}) leads to a nonexistent folder.${
                EOL }Remember to use the argument '--luisFolder' for your Skill's LUIS folder.`);
        });

        it("when the .lu file path leads to a nonexistent file when using manifest v1", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v1", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: Path to the connectableSkill.lu file leads to a nonexistent file.${
                EOL }Make sure your Skill's .lu file's name matches your Skill's manifest id`);
        });

        it("when the .lu file path leads to a nonexistent file when using manifest v2", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: Path to the LU file (${resolve(__dirname, join("mocks", "success", "en-us", "testSkill.lu"))}) leads to a nonexistent file.`);
        });

        it("when the dispatch folder path leads to a nonexistent folder", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "fail", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: Path to the Dispatch folder (${ join(configuration.dispatchFolder, configuration.languages[0]) }) leads to a nonexistent folder.${
                EOL }Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        });

        it("when the path to dispatch file doesn't exist", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "lu")),
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithNoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: Path to the nonExistenceen-usDispatch.dispatch file leads to a nonexistent file.`);
        });

        it("when the .luis file path leads to a nonexistent file", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "lu")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: There was an error in the bf luis:convert command:${
                EOL }Command: bf luis:convert --in "${join(configuration.luisFolder, configuration.languages[0], "testSkill.lu")}" --culture "${configuration.languages[0]}" --out "${join(configuration.luisFolder, configuration.languages[0], 'testSkill.luis')}" --name "testSkill" --force${
                EOL }Error: Path to testSkill.luis (${join(configuration.luisFolder, configuration.languages[0], "testSkill.luis")}) leads to a nonexistent file.`);
        });

        it("when the dispatch add command fails", async function () {
            sandbox.replace(this.connector, "executeLuisConvert", () => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "runCommand", () => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: There was an error in the dispatch add command:${
                EOL }Command: dispatch add --type file --name testSkill --filePath ${join(configuration.luisFolder, configuration.languages[0], "testSkill.luis")} --intentName testSkill --dataFolder ${join(configuration.dispatchFolder, configuration.languages[0])} --dispatch ${join(configuration.dispatchFolder, configuration.languages[0], "filleden-usDispatch.dispatch") +
                EOL }Error: Mocked function throws an Error`);
        });

        it("when languages argument contains non-supported cultures for the VA", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["zh-hk"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: Some of the cultures provided to connect from the Skill are not available or aren't supported by your VA.${
                EOL }Make sure you have a Dispatch for the cultures you are trying to connect, and that your Skill has a LUIS model for that culture`);
        });

        it("when the execution of an external command fails", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: There was an error in the bf luis:convert command:${
                EOL }Command: bf luis:convert --in "${join(configuration.luisFolder, configuration.languages[0], "testSkill.lu")}" --culture "${configuration.languages[0]}" --out "${join(configuration.luisFolder, configuration.languages[0], 'testSkill.luis')}" --name "testSkill" --force${
                EOL }Error: The execution of the bf command failed with the following error:${
                EOL }Error: Mocked function throws an Error`);
		});

        it("when the refresh execution fails", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "executeRefresh", () => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:${
                EOL }Error: An error ocurred while updating the Dispatch model:${
                EOL }Error: Mocked function throws an Error`);
        });

        it("The localManifest V1 points to a nonexisting Endpoint URL", async function() {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v1", "invalidEndpoint.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };
        
            const errorMessages = [
                `Missing property 'endpoint' of the manifest`,
                `There was an error while connecting the Skill to the Assistant:${
                    EOL }Error: One or more properties are missing from your Skill Manifest`
            ]
        
            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();
        
            errorList.forEach((errorMessage, index) => {
                strictEqual(errorMessage, errorMessages[index]);
            });
        });

        it("The localManifest V2 points to a nonexisting Endpoint URL", async function() {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "invalidEndpoint.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };
        
            const errorMessages = [
                `Missing property 'endpointUrl' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`,
                `There was an error while connecting the Skill to the Assistant:${
                    EOL }Error: One or more properties are missing from your Skill Manifest`
            ]
        
            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();
        
            errorList.forEach((errorMessage, index) => {
                strictEqual(errorMessage, errorMessages[index]);
            });
        });
    });

    describe("should show a warning", function () {
        it("when the Skill is already connected", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const warningList = this.logger.getWarning();
            
			strictEqual(warningList[warningList.length - 1], `The skill with ID 'testSkill' is already registered.`);
        });

        it("when the noRefresh argument is present", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "executeRefresh", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                noRefresh: true,
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const warningList = this.logger.getWarning();

            strictEqual(warningList[warningList.length - 1], `Run 'botskills refresh --${configuration.lgLanguage}' command to refresh your connected skills`);
        });

        it("when a wildcard intent is included in the skill manifest", async function () {
            sandbox.replace(this.connector, "executeDispatchAdd", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "executeRefresh", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifestWithWildcardIntent.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "authConnectionAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const warningMessage = this.logger.getWarning();

			strictEqual(warningMessage[0], `Found intent with name '*'. Adding all intents.`);
		});
    });

    describe("should show a message", function () {
        it("when the skill is successfully connected to the Assistant", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "executeRefresh", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const messageList = this.logger.getMessage();

			strictEqual(messageList[messageList.length - 1], `Appending 'Test Skill' manifest to your assistant's skills configuration file.`);
		});
    });
});
