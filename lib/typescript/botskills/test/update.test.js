/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require('assert');
const fs = require('fs');
const { join, resolve } = require('path');
const sandbox = require('sinon').createSandbox();
const testLogger = require('./helpers/testLogger');
const botskills = require('../lib/index');
let logger;
let updater;

describe("The update command", function () {
    beforeEach(function () {
        fs.writeFileSync(resolve(__dirname, join('mocks', 'resources', 'filledSkillsArray.json')),
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
        updater = new botskills.UpdateSkill(logger);
    });
    
    describe("should show an error", function () {
        it("when the skill to update is not present in the assistant manifest", async function() {
            const config = {
                botName: 'mock-assistant',
                localManifest: resolve(__dirname, join('mocks', 'resources', 'absentSkill.json')),
                remoteManifest: '',
                dispatchName: '',
                language: '',
                luisFolder: resolve(__dirname, join('mocks', 'resources')),
                dispatchFolder: '',
                outFolder: '',
                lgOutFolder: '',
                skillsFile: resolve(__dirname, join('mocks', 'resources', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: 'ts',
                logger: logger
            };

            await updater.updateSkill(config);
            const ErrorList = logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while updating the Skill from the Assistant:
Error: The Skill doesn't exist in the Assistant, run 'botskills connect --botName ${config.botName} --localManifest "${config.localManifest}" --luisFolder "${config.luisFolder}" --${config.lgLanguage}'`);
        });

        it("when the localManifest points to a nonexisting Skill manifest file", async function () {
            const config = {
                botName: '',
                localManifest: resolve(__dirname, join('mocks', 'resources', 'nonexistentSkill.json')),
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
            await updater.updateSkill(config);
            const ErrorList = logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while updating the Skill from the Assistant:
Error: The 'localManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`);
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
            await updater.updateSkill(config);
            const ErrorList = logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while updating the Skill from the Assistant:
RequestError: Error: getaddrinfo ENOTFOUND nonexistentskill.azurewebsites.net nonexistentskill.azurewebsites.net:80`);            
        });
    });

    describe("should show a success message", function () {
        it("when the skill is successfully updated to the Assistant", async function () {
            const config = {
                skillId: 'testSkill',
                botName: '',
                localManifest: resolve(__dirname, join('..', 'mockedFiles', 'testSkill.json')),
                remoteManifest: '',
                dispatchName: 'testSkill',
                language: '',
                luisFolder: resolve(__dirname, join('mocks', 'resources', 'successfulConnectFiles')),
                dispatchFolder: resolve(__dirname, join('mocks', 'resources', 'successfulConnectFiles')),
                outFolder: '',
                lgOutFolder: '',
                skillsFile: resolve(__dirname, join('mocks', 'resources', 'filledSkillsArray.json')),
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: 'ts',
                logger: logger
            };
            sandbox.replace(updater.disconnectSkill, 'disconnectSkill', () => {
                return Promise.resolve('Mocked function successfully');
            })
            sandbox.replace(updater.connectSkill, 'connectSkill', () => {
                return Promise.resolve('Mocked function successfully');
            })

            await updater.updateSkill(config);
            const SuccessList = logger.getSuccess();
            assert.strictEqual(SuccessList[SuccessList.length - 1], `Successfully updated '${config.skillId}' skill from your assistant's skills configuration file.`);
        });
    });
});
