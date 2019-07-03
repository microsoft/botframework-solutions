/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { writeFileSync } = require("fs");
const { join, resolve } = require("path");
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");

describe("The list command", function () {

    beforeEach(function () {
        writeFileSync(resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
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
        this.lister = new botskills.ListSkill(this.logger);
    })

	describe("should show an error", function () {		
        it("when there is no skills File", async function () {
            const config = {
                skillsFile: "",
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);
        });

        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "badSkills.jso"),
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while listing the Skills connected to your assistant:
 SyntaxError: Unexpected token N in JSON at position 0`);
		});
    });

    describe("should show a message", function () {
        it("when there is no skills connected to the assistant", async function () {
            const config = {
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "emptySkills.json"),
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const messageList = this.logger.getMessage();

			strictEqual(messageList[messageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there is no skills array defined in the Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "undefinedSkills.json"),
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const messageList = this.logger.getMessage();

			strictEqual(messageList[messageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there is a skill in the Assistant Skills configuration file", async function () {
            const config = {
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const messageList = this.logger.getMessage();
            
            strictEqual(messageList[messageList.length - 1], `The skills already connected to the assistant are the following:
\t- testSkill
\t- testDispatch`);
		});
	});
});
