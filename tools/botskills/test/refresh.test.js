/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");

describe("The refresh command", function () {
    
    beforeEach(function () {
        this.logger = new testLogger.TestLogger();
        this.refresher = new botskills.RefreshSkill(this.logger);
    });
    
    describe("should show an error", function() {
        it("when the dispatchFolder points to a nonexistent folder", async function () {
            const config = {
                dispatchName : "",
                dispatchFolder : resolve(__dirname, "mocks", "fail", "nonexistentDispatch"),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.refresher.refreshSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:
Error: Path to the Dispatch folder (${config.dispatchFolder}) leads to a nonexistent folder.
Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        });

        it("when the dispatchName points to a nonexistent file", async function () {
            const config = {
                dispatchName : "nonexistentDispatch",
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.refresher.refreshSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:
Error: Path to the ${config.dispatchName}.dispatch file leads to a nonexistent file.
Make sure to use the argument '--dispatchName' for your Assistant's Dispatch file name.`);
        });

        it("when the external calls fails", async function () {
            sandbox.replace(this.refresher, "updateDispatch", (configuration) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.refresher.childProcessUtils, "execute", (command, args) => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const config = {
                dispatchName:  "connectableSkill",
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: "",
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.refresher.refreshSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:
Error: There was an error in the luisgen command:
Command: luisgen ${join(config.dispatchFolder, config.dispatchName)}.json -cs "DispatchLuis" -o 
Error: Mocked function throws an Error`);
        });
    });

    describe("should show a successfully message", function() {
        it("when the refreshSkill is executed successfully", async function () {
            sandbox.replace(this.refresher.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const config = {
                dispatchName : "connectableSkill",
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.refresher.refreshSkill(config);
            const successList = this.logger.getSuccess();

            strictEqual(successList[successList.length - 1], `Successfully refreshed Dispatch model`);
        });
    });
});
