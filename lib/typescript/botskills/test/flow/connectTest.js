/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const sandbox = require('sinon').createSandbox();
const testLogger = require('../models/testLogger');
const botskills = require('../../lib/index');
let logger;
let connector;

describe("The connect command", function () {

    beforeEach(function () {
        fs.writeFileSync(path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
        JSON.stringify(
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
        logger = new testLogger.TestLogger();
        connector = new botskills.ConnectSkill(logger);
    })

	describe("should show an error", function () {
        it("when there is no skills File", async function () {
            const config = {
                botName: '',
                localManifest: '',
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: '',
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: '',
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };

            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: Either the 'localManifest' or 'remoteManifest' argument should be passed.`);
        });

        it("when the localManifest points to a nonexisting Skill manifest file", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'nonexistentSkill.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: '',
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: '',
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: The 'localManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`);
        });

        it("when the Skill is missing all mandatory fields", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'invalidSkill.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: '',
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: '',
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            const errorMessages = [
`Missing property 'name' of the manifest`,
`Missing property 'id' of the manifest`,
`Missing property 'endpoint' of the manifest`,
`Missing property 'authenticationConnections' of the manifest`,
`Missing property 'actions' of the manifest`
            ]
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
            ErrorList.forEach((errorMessage, index) => {
                assert.strictEqual(errorMessage, errorMessages[index]);
            });
        });

        it("when the Skill has an invalid id field", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'invalidSkillid.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: '',
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: '',
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };

            await connector.connectSkill(config);
            const ErrorList = logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'id' of the manifest contains some characters not allowed. Make sure the 'id' contains only letters, numbers and underscores, but doesn't start with number.`);
        });

        it("when the remoteManifest points to a nonexisting Skill manifest URL", async function() {
            const config = {
                botName: '',
                localManifest: '',
                remoteManifest: 'http://nonexistentSkill.azurewebsites.net/api/skill/manifest',
                dispatchName: '',
                language: '',
                luisFolder: '',
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: '',
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nRequestError: Error: getaddrinfo ENOTFOUND nonexistentskill.azurewebsites.net nonexistentskill.azurewebsites.net:80`);
        });

        it("when the luisFolder leads to a nonexistent folder", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'connectableSkill.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'nonexistentLuisFolder')),
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Path to the LUIS folder (${config.luisFolder}) leads to a nonexistent folder.`);
        });

        it("when the .lu file path leads to a nonexistent file", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'connectableSkill.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: path.resolve(__dirname, path.join('..', 'mockedFiles')),
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Path to the connectableSkill.lu file leads to a nonexistent file.`);
        });

        it("when the dispatch folder path leads to a nonexistent folder", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'connectableSkill.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'luisFolder')),
                dispatchFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'nonexistentDispatchFolder')),
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Path to the Dispatch folder (${config.dispatchFolder}) leads to a nonexistent folder.`);
        });

        it("when the .dispatch file path leads to a nonexistent file", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'connectableSkill.json')),
                remoteManifest: '',
                dispatchName: 'nonexistentDispatchFile',
                language: '',
                luisFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'luisFolder')),
                dispatchFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'dispatchFolder')),
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Path to the ${config.dispatchName}.dispatch file leads to a nonexistent file.`);
        });

        it("when the .luis file path leads to a nonexistent file", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'connectableSkill.json')),
                remoteManifest: '',
                dispatchName: 'connectableSkill',
                language: '',
                luisFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'luFolder')),
                dispatchFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'dispatchFolder')),
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            sandbox.replace(connector.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Path to ${config.dispatchName}.luis (${path.join(config.luisFolder, config.dispatchName)}.luis) leads to a nonexistent file. Make sure the ludown command is being executed successfully`);
        });

        it("when the dispatch refresh fails and the dispatch .json file is missing", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'connectableSkill.json')),
                remoteManifest: '',
                dispatchName: 'connectableSkill',
                language: '',
                luisFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'luisFolder')),
                dispatchFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'dispatchFolder')),
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            sandbox.replace(connector.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            await connector.connectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Path to ${config.dispatchName}.json (${path.join(config.dispatchFolder, config.dispatchName)}.json) leads to a nonexistent file. Make sure the dispatch refresh command is being executed successfully`);
        });
    });

    describe("should show a warning", function () {
        it("when the Skill is already connected", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'testSkill.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: '',
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            await connector.connectSkill(config);
            const WarningList = logger.getWarning();
			assert.strictEqual(WarningList[WarningList.length - 1], `The skill 'Test Skill' is already registered.`);
        });

    });

    describe("should show a message", function () {
        it("when the skill is successfully connected to the Assistant", async function () {
            const config = {
                botName: '',
                localManifest: path.resolve(__dirname, path.join('..', 'mockedFiles', 'connectableSkill.json')),
                remoteManifest: '',
                dispatchName: 'connectableSkill',
                language: '',
                luisFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'successfulConnectFiles')),
                dispatchFolder: path.resolve(__dirname, path.join('..', 'mockedFiles', 'successfulConnectFiles')),
                outFolder: '',
                lgOutFolder: '',
                skillsFile: path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: logger
            };
            sandbox.replace(connector.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            sandbox.replace(connector.authenticationUtils, 'authenticate', (configuration, manifest, logger) => {
                return Promise.resolve('Mocked function successfully');
            });
            await connector.connectSkill(config);
            const MessageList = logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `Configuring bot auth settings`);
		});
	});
});
