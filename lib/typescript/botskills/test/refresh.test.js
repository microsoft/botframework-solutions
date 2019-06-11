/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const { join, resolve } = require("path");
const sandbox = require('sinon').createSandbox();
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");
let childProcessStub;

describe("The refresh command", function () {
    afterEach(function (){
        sandbox.restore();
    });

    beforeEach(function () {
        this.logger = new testLogger.TestLogger();
        this.refresher = new botskills.RefreshSkill(this.logger);
    });

    describe("should show an error", function() {
        it("when the dispatchFolder points to a nonexistent folder", async function () {
            const config = {
                dispatchName : "",
                dispatchFolder : resolve(__dirname, '..', 'nonexistentDispatchFolder'),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, '..', 'mocks', 'resources'),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.refresher.refreshSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while refreshing any Skill from the Assistant:
Error: Path to the Dispatch folder (${config.dispatchFolder}) leads to a nonexistent folder.`);
        });

        it("when the dispatchName points to a nonexistent file", async function () {
            const config = {
                dispatchName : "filledDispatchNoJson",
                dispatchFolder : resolve(__dirname, join('mocks', 'resources', "dispatchFolder")),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "..", 'mocks', 'resources'),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.refresher.refreshSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while refreshing any Skill from the Assistant:
Error: Path to the ${config.dispatchName}.dispatch file leads to a nonexistent file.`);
        });

        it("when the external calls fails", async function () {
            sandbox.replace(this.refresher, 'updateDispatch', (configuration) => {
                return Promise.resolve("Mocked function successfully");
            });

            sandbox.replace(this.refresher.childProcessUtils, 'execute', (command, args) => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });

            const config = {
                dispatchName:  "connectableSkill",
                dispatchFolder : resolve(__dirname, join('mocks', 'resources', "dispatchFolder")),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: "",
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.refresher.refreshSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while refreshing any Skill from the Assistant:
Error: There was an error in the luisgen command:
Error: Mocked function throws an Error`);
        });
    });

    describe("should show a successfully message", function() {
        it("when the refreshSkill is executed successfully", async function () {
            const config = {
                dispatchName : "connectableSkill",
                dispatchFolder : resolve(__dirname, join('mocks', 'resources', "successfulConnectFiles")),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, 'mocks', 'resources'),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            sandbox.replace(this.refresher.childProcessUtils, 'execute', (command, args) => {
                return Promise.resolve('Mocked function successfully');
            });
            await this.refresher.refreshSkill(config);
            const SuccessList = this.logger.getSuccess();
            assert.strictEqual(SuccessList[SuccessList.length - 1], `Successfully refreshed Dispatch model`);
        });
    });
});
