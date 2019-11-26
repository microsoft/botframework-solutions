/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { writeFileSync } = require("fs");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");
const { normalizeContent } = require("./helpers/normalizeUtils");
const botskills = require("../lib/index");
const filledSkills = normalizeContent(JSON.stringify(
    {
        "skills": [
            {
                "id": "testSkill"
            },
            {
                "id": "testDispatch"
            }
        ]
    },
    null, 4));

function undoChangesInTemporalFiles() {
    writeFileSync(resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")), filledSkills);
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
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: Either the 'localManifest' or 'remoteManifest' argument should be passed.`);
        });

        it("when there is no cognitiveModels file", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifestWithTwoLanguages.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "nonCognitiveModels.json"),
                lgLanguage: "",
                logger: this.logger
            };
            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();
            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: Could not find the cognitiveModels file (${configuration.cognitiveModelsFile}). Please provide the '--cognitiveModelsFile' argument.`);
        });


        it("when the localManifest points to a nonexisting Skill manifest file", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "nonexistentSkill.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: The 'localManifest' argument leads to a non-existing file.
Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        });

        it("when the Skill is missing all mandatory fields", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "invalidManifest.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            const errorMessages = [
                `Missing property 'name' of the manifest`,
                `Missing property 'id' of the manifest`,
                `Missing property 'endpoint' of the manifest`,
                `Missing property 'authenticationConnections' of the manifest`,
                `Missing property 'actions' of the manifest`
            ]

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            errorList.forEach((errorMessage, index) => {
                strictEqual(errorMessage, errorMessages[index]);
            });
        });

        it("when the Skill has an invalid id field", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "invalidIdManifest.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `The 'id' of the manifest contains some characters not allowed. Make sure the 'id' contains only letters, numbers and underscores, but doesn't start with number.`);
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
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
RequestError: Error: getaddrinfo ENOTFOUND nonexistentskill.azurewebsites.net nonexistentskill.azurewebsites.net:80`);
        });

        it("when the luisFolder leads to a nonexistent folder", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "fail", "nonexistentLuis")),
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: Path to the LUIS folder (${configuration.luisFolder}) leads to a nonexistent folder.
Remember to use the argument '--luisFolder' for your Skill's LUIS folder.`);
        });

        it("when the .lu file path leads to a nonexistent file", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success")),
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: Path to the connectableSkill.lu file leads to a nonexistent file.
Make sure your Skill's .lu file's name matches your Skill's manifest id`);
        });

        it("when the dispatch folder path leads to a nonexistent folder", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "fail", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: Path to the Dispatch folder (${configuration.dispatchFolder}\\${configuration.languages[0]}) leads to a nonexistent folder.
Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        });

        it("when the path to dispatch file doesn't exist", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "lu")),
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithNoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: Path to the nonExistenceen-usDispatch.dispatch file leads to a nonexistent file.`);
        });

        it("when the .luis file path leads to a nonexistent file", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "lu")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: There was an error in the ludown parse command:
Command: ludown parse toluis --in "${join(configuration.luisFolder, configuration.languages[0], "connectableSkill.lu")}" --luis_culture ${configuration.languages[0]} --out_folder "${join(configuration.luisFolder, configuration.languages[0])}" --out connectableSkill.luis
Error: Path to connectableSkill.luis (${join(configuration.luisFolder, configuration.languages[0], "connectableSkill.luis")}) leads to a nonexistent file.`);
        });

        it("when the dispatch add command fails", async function () {
            sandbox.replace(this.connector, "executeLudownParse", () => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "runCommand", () => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifestWithTwoLanguages.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: There was an error in the dispatch add command:
Command: dispatch add --type file --name connectableSkill --filePath ${join(configuration.luisFolder, configuration.languages[0], "connectableSkill.luis")} --intentName connectableSkill --dataFolder ${join(configuration.dispatchFolder, configuration.languages[0])} --dispatch ${join(configuration.dispatchFolder, configuration.languages[0], "filleden-usDispatch.dispatch")}
Error: Mocked function throws an Error`);
        });

        it("when languages argument contains non-supported cultures for the VA", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["zh-hk"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: Some of the cultures provided to connect from the Skill are not available or aren't supported by your VA.
Make sure you have a Dispatch for the cultures you are trying to connect, and that your Skill has a LUIS model for that culture`);
        });

        it("when the execution of an external command fails", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifestWithTwoLanguages.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: There was an error in the ludown parse command:
Command: ludown parse toluis --in "${join(configuration.luisFolder, configuration.languages[0], "connectableSkill.lu")}" --luis_culture ${configuration.languages[0]} --out_folder "${join(configuration.luisFolder, configuration.languages[0])}" --out connectableSkill.luis
Error: The execution of the ludown command failed with the following error:
Error: Mocked function throws an Error`);
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
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while connecting the Skill to the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: Mocked function throws an Error`);
        });
    });

    describe("should show a warning", function () {
        it("when the Skill is already connected", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "repeatedManifest.json")),
                remoteManifest: "",
                languages: ["en-us"],
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const warningList = this.logger.getWarning();
            
			strictEqual(warningList[warningList.length - 1], `The skill 'Test Skill' is already registered.`);
        });

        it("when the noRefresh argument is present", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector.authenticationUtils, "authenticate", (configuration, manifest, logger) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "executeRefresh", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifest.json")),
                remoteManifest: "",
                noRefresh: true,
                languages: ["en-us"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const warningList = this.logger.getWarning();

            strictEqual(warningList[warningList.length - 1], `Run 'botskills refresh --${configuration.lgLanguage}' command to refresh your connected skills`);
        });
    });

    describe("should show a message", function () {
        it("when the skill is successfully connected to the Assistant", async function () {
            sandbox.replace(this.connector.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector.authenticationUtils, "authenticate", (configuration, manifest, logger) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.connector, "executeRefresh", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "connectableManifestWithTwoLanguages.json")),
                remoteManifest: "",
                languages: ["en-us", "es-es"],
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "",
                logger: this.logger
            };

            this.connector.configuration = configuration;
            await this.connector.connectSkill();
            const messageList = this.logger.getMessage();

			strictEqual(messageList[messageList.length - 1], `Configuring bot auth settings`);
		});
	});
});
