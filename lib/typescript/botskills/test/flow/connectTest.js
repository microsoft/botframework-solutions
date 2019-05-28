/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require('assert');
const fs = require('fs');
const path = require('path');
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
                }
                , null, 4));
        this.logger = new testLogger.TestLogger();
        this.connector = new botskills.ConnectSkill(this.logger);
    })

	describe("should show an error", function () {
        it("when there's no skills File", async function () {
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
                logger: this.logger
            };

            await this.connector.connectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while connecting the Skill to the Assistant:\nError: Either the 'localManifest' or 'remoteManifest' argument should be passed.`);
        });

        it("when the localManifest points to a bad formatted Assistant Skills configuration file", async function () {
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
                skillsFile: '',
                resourceGroup: '',
                appSettingsFile: '',
                cognitiveModelsFile: '',
                lgLanguage: '',
                logger: this.logger
            };

            await this.connector.connectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while listing the Skills connected to your assistant:\n Error: The 'localManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`);
		});
    });

    describe("should show a warning", function () {

    });

    xdescribe("should show a message", function () {
        it("when there's no skills connected to the assistant", async function () {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/emptySkillsArray.json'),
                logger: this.logger
            };

            await this.connector.connectSkill(config);
            const MessageList = this.logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there's no skills array defined in the Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/undefinedSkillsArray.json'),
                logger: this.logger
            };

            await this.connector.connectSkill(config);
            const MessageList = this.logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there's a skill in the Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                logger: this.logger
            };

            await this.connector.connectSkill(config);
            const MessageList = this.logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `The skills already connected to the assistant are the following:\n\t- testSkill\n\t- testDispatch`);
		});
	});
});
