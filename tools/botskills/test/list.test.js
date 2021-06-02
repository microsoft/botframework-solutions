/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join, resolve } = require("path");
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");
const { EOL } = require('os');

describe("The list command", function () {

    beforeEach(function () {
        this.logger = new testLogger.TestLogger();
        this.lister = new botskills.ListSkill(this.logger);
    });

	describe("should show an error", function () {		
        it("when there is no appsettings File", async function () {
            const config = {
                logger: this.logger,
                appSettingsFile: ""
            };

            await this.lister.listSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `The 'appSettingsFile' argument is absent or leads to a non-existing file.${
                EOL }Please make sure to provide a valid path to your Assistant Skills configuration file using the '--appSettingsFile' argument.`);
        });
    });

    describe("should show a message", function () {
        it("when there is no skills connected to the assistant", async function () {
            const config = {
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")),
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const messageList = this.logger.getMessage();

			strictEqual(messageList[messageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there is no skills array defined in the Assistant Skills configuration file", async function () {
            const config = {
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithEmptySkills.json")),
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const messageList = this.logger.getMessage();

			strictEqual(messageList[messageList.length - 1], `There are no Skills connected to the assistant.`);
        });

        it("when there is a skill in the Assistant Skills configuration file", async function () {
            const config = {
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")),
                logger: this.logger
            };

            await this.lister.listSkill(config);
            const messageList = this.logger.getMessage();
            
            strictEqual(messageList[messageList.length - 1], `The skills already connected to the assistant are the following:${ EOL }\t- testSkill`);
		});
	});
});
