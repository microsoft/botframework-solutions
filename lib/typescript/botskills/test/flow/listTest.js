/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require('assert');
const path = require('path');
const testLogger = require('../models/testLogger');
const Botskills = require('../../lib/index');

describe("The list command", function () {
    let logger;

    beforeEach(function () {
        logger = new testLogger.TestLogger();
    })

	describe("should show an error", function () {
		

        it("when there's no skills File", function (done) {
            const config = {
                skillsFile: '',
                logger: logger
            };

            Botskills.listSkill(config);
			assert.strictEqual(logger.getError(), `The 'skillsFile' argument is absent or leads to a non-existing file.\nPlease make sure to provide a valid path to your Assistant Skills configuration file.`);

            done();
        });

        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", function (done) {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/badSkillsArray.jso'),
                logger: logger
            };

            Botskills.listSkill(config);
			assert.strictEqual(logger.getError(), `There was an error while listing the Skills connected to your assistant:\n SyntaxError: Unexpected identifier`);

            done();
		});
    });

    describe("should show a warning", function () {

    });

    describe("should show a message", function () {
        it("when there's no skills connected to the assistant", function (done) {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/emptySkillsArray.json'),
                logger: logger
            };

            Botskills.listSkill(config);
			assert.strictEqual(logger.getMessage(), `There are no Skills connected to the assistant.`);

            done();
        });

        it("when there's no skills array defined in the Assistant Skills configuration file", function (done) {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/undefinedSkillsArray.json'),
                logger: logger
            };

            Botskills.listSkill(config);
			assert.strictEqual(logger.getMessage(), `There are no Skills connected to the assistant.`);

            done();
        });

        it("when there's a skill in the Assistant Skills configuration file", function (done) {
            const config = {
                skillsFile: path.resolve(__dirname, '../mockedFiles/filledSkillsArray.json'),
                logger: logger
            };

            Botskills.listSkill(config);
			assert.strictEqual(logger.getMessage(), `The skills already connected to the assistant are the following:\n\t- testSkill`);

            done();
		});
	});
});
