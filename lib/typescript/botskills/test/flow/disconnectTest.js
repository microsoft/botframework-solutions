/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const sinon = require('sinon');
const sandbox = require('sinon').createSandbox();
const testLogger = require('../models/testLogger');
const botskills = require('../../lib/index');
let logger;

describe("The disconnect command", function () {
    
    beforeEach(function () {
        this.logger = new testLogger.TestLogger();
        disconnector = new botskills.DisconnectSkill(this.logger);
        fs.writeFileSync(path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledDispatch.dispatch')),
            JSON.stringify(
                {
                    "services": [
                        {
                            "name": "testDispatch",
                            "id": "1"
                        }
                    ],
                    "serviceIds": [
                        "1"
                    ]
                }
                , null, 4));
        fs.writeFileSync(path.resolve(__dirname, path.join('..', 'mockedFiles', 'filledDispatchNoJson.dispatch')),
            JSON.stringify(
                {
                    "services": [
                        {
                            "name": "testDispatch",
                            "id": "1"
                        }
                    ],
                    "serviceIds": [
                        "1"
                    ]
                }
                , null, 4));
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
                }
                , null, 4));
    })

	describe("should show an error", function () {
        it("when there is no skills File", async function () {
            const config = {
                skillId : "",
                skillsFile : "",
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder: path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'skillsFile' argument is absent or leads to a non-existing file.\nPlease make sure to provide a valid path to your Assistant Skills configuration file.`);
        });

        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/badSkillsArray.jso'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder: path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:\nSyntaxError: Unexpected identifier`);
        });

        it("when the dispatchName and dispatchFolder point to a nonexistent file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder: path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "missingDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `Could not find file ${config.dispatchName}.dispatch. Please provide the 'dispatchName' and 'dispatchFolder' parameters.`);
        });

        it("when the dispatch refresh fails to create the Dispatch JSON file", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder: path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "filledDispatchNoJson",
                lgLanguage : "cs",
                logger : this.logger
            };

            sandbox.replace(disconnector.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Path to ${config.dispatchName}.json (${path.join(config.dispatchFolder, config.dispatchName)}.json) leads to a nonexistent file. Make sure the dispatch refresh command is being executed successfully`);
        });

        it("when the lgOutFolder argument is invalid ", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder : path.resolve(__dirname, '../misleadingMockedFiles'),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            sandbox.replace(disconnector.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'lgOutFolder' argument is absent or leads to a non-existing folder.\nPlease make sure to provide a valid path to your LUISGen output folder.`);
        });

        it("when the lgLanguage argument is invalid", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder : path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "filledDispatch",
                lgLanguage : "",
                logger : this.logger
            };

            sandbox.replace(disconnector.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'lgLanguage' argument is incorrect.\nIt should be either 'cs' or 'ts' depending on your assistant's language.`);
        });

        it("when the execution of a command fails", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder : path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            sandbox.replace(disconnector.childProcessUtils, 'execute', (command, args) => {
                return Promise.reject(new Error('Mocked function throws an Error'));
            });
            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:\nError: An error ocurred while updating the Dispatch model:\nError: Mocked function throws an Error`);
        });

    });

    describe("should show a warning", function () {
        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/emptySkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder: path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const WarningList = this.logger.getWarning();
			assert.strictEqual(WarningList[WarningList.length - 1], `The skill 'testSkill' is not present in the assistant Skills configuration file.\nRun 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
        });

        it("when the dispatchName is not contained in the Dispatch file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder: path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const WarningList = this.logger.getWarning();
			assert.strictEqual(WarningList[WarningList.length - 1], `The skill ${config.skillId} is not present in the Dispatch model.\nRun 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
		});
    });

    describe("should show a message", function () {
        
    });

    describe("should show a success message", function () {
        it("when the skill is successfully disconnected", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder : path.resolve(__dirname, '../mockedFiles'),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            sandbox.replace(disconnector.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            await disconnector.disconnectSkill(config);
            const SuccessList = this.logger.getSuccess();
			assert.strictEqual(SuccessList[SuccessList.length - 1], `Successfully removed '${config.skillId}' skill from your assistant's skills configuration file.`);
        });
	});
});
