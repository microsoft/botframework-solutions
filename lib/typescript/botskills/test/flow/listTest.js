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

describe("The list command", function () {

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
        lister = new botskills.ListSkill(this.logger);
    })

	describe("should show an error", function () {
		

        it("when there is no skills File", async function () {
            const config = {
                skillsFile: '',
                logger: this.logger
            };

            await lister.listSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'skillsFile' argument is absent or leads to a non-existing file.\nPlease make sure to provide a valid path to your Assistant Skills configuration file.`);
        });

        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/badSkillsArray.jso'),
                logger: this.logger
            };

            await lister.listSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while listing the Skills connected to your assistant:\n SyntaxError: Unexpected token N in JSON at position 0`);
		});
    });

    describe("should show a warning", function () {

    });

    describe("should show a message", function () {
        it("when there is no skills connected to the assistant", async function () {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/emptySkillsArray.json'),
                logger: this.logger
            };

            await lister.listSkill(config);
            const MessageList = this.logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there is no skills array defined in the Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/undefinedSkillsArray.json'),
                logger: this.logger
            };

            await lister.listSkill(config);
            const MessageList = this.logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there is a skill in the Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                logger: this.logger
            };

            await lister.listSkill(config);
            const MessageList = this.logger.getMessage();
			assert.strictEqual(MessageList[MessageList.length - 1], `The skills already connected to the assistant are the following:\n\t- testSkill\n\t- testDispatch`);
		});
	});
});
