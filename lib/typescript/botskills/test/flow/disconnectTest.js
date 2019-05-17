/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require('assert');
const path = require('path');
const sinon = require('sinon');
const sandbox = require('sinon').createSandbox();
const testLogger = require('../models/testLogger');
const Botskills = require('../../lib/index');

describe("The disconnect command", function () {
    let logger;

    beforeEach(function () {
        logger = new testLogger.TestLogger();
    })

	describe("should show an error", function () {
        it("when there's no skills File", function (done) {
            const config = {
                skillId : "",
                skillsFile : "",
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'skillsFile' argument is absent or leads to a non-existing file.\nPlease make sure to provide a valid path to your Assistant Skills configuration file.`);

            done();
        });

        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", function (done) {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/badSkillsArray.jso'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:\nSyntaxError: Unexpected identifier`);

            done();
        });

        it("when the dispatchName and dispatchFolder point to a nonexistent file", function (done) {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
        
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder : "",
                dispatchName : "missingDispatch",
                lgLanguage : "",
                logger : logger
            };

            // sandbox.replace(Botskills, 'execute', (command, args) => {
            //     return new Promise((pResolve, pReject) => {
            //         pResolve('Test mocking execute method');
            //     });
            //     // return logger.error('Test mocking execute method');
            // });
            Botskills.disconnectSkill(config);
            const ErrorList = logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `Could not find file ${config.dispatchName}.dispatch. Please provide the 'dispatchName' and 'dispatchFolder' parameters.`);

            done();
        });
    });

    describe("should show a warning", function () {
        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", function (done) {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/emptySkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const WarningList = logger.getWarning();
			assert.strictEqual(WarningList[WarningList.length - 1], `The skill 'testSkill' is not present in the assistant Skills configuration file.\nRun 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

            done();
        });

        it("when the dispatchName is not contained in the Dispatch file", function (done) {
            const config = {
                skillId : "testSkill",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : path.resolve(__dirname, '../mockedFiles'),
                lgOutFolder : "",
                dispatchName : "filledDispatch",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const WarningList = logger.getWarning();
			assert.strictEqual(WarningList[WarningList.length - 1], `The skill ${config.skillId} is not present in the Dispatch model.\nRun 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

            done();
		});
    });

    xdescribe("should show a message", function () {
        it("when there's no skills connected to the assistant", function (done) {
            const config = {
                skillId : "",
                skillsFile: path.resolve(__dirname, '../mockedFiles/emptySkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const MessageList = logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `There are no Skills connected to the assistant.`);

            done();
        });

        it("when there's no skills array defined in the Assistant Skills configuration file", function (done) {
            const config = {
                skillId : "",
                skillsFile: path.resolve(__dirname, '../mockedFiles/undefinedSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const MessageList = logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `There are no Skills connected to the assistant.`);

            done();
        });

        it("when there's a skill in the Assistant Skills configuration file", function (done) {
            const config = {
                skillId : "",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const MessageList = logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `The skills already connected to the assistant are the following:\n\t- testSkill`);

            done();
		});
    });

    xdescribe("should show a success message", function () {
        it("when there's no skills connected to the assistant", function (done) {
            const config = {
                skillId : "",
                skillsFile: path.resolve(__dirname, '../mockedFiles/emptySkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const SuccessList = logger.getSuccess();
			assert.strictEqual(SuccessList[SuccessList.length - 1], `There are no Skills connected to the assistant.`);

            done();
        });

        it("when there's no skills array defined in the Assistant Skills configuration file", function (done) {
            const config = {
                skillId : "",
                skillsFile: path.resolve(__dirname, '../mockedFiles/undefinedSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const SuccessList = logger.getSuccess();
			assert.strictEqual(SuccessList[SuccessList.length - 1], `There are no Skills connected to the assistant.`);

            done();
        });

        it("when there's a skill in the Assistant Skills configuration file", function (done) {
            const config = {
                skillId : "",
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : "",
                dispatchName : "",
                lgLanguage : "",
                logger : logger
            };

            Botskills.disconnectSkill(config);
            const SuccessList = logger.getSuccess();
			assert.strictEqual(SuccessList[SuccessList.length - 1], `The skills already connected to the assistant are the following:\n\t- testSkill`);

            done();
		});
	});
});
