/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");
const { EOL } = require('os');

describe("The refresh command", function () {
    
    beforeEach(function () {
        this.logger = new testLogger.TestLogger();
        this.refresher = new botskills.RefreshSkill();
        this.refresher.logger = this.logger;
    });
    
    describe("should show an error", function() {
        it("when there is no cognitiveModels file", async function () {
            const configuration = {
                dispatchFolder : resolve(__dirname, "mocks", "fail", "nonexistentDispatch"),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "nonCognitiveModels.json"),
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                logger: this.logger
            };

            this.refresher.configuration = configuration;
            await this.refresher.refreshSkill(configuration);
            const errorList = this.logger.getError();
            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:${
                EOL }Error: Could not find the cognitiveModels file (${configuration.cognitiveModelsFile}). Please provide the '--cognitiveModelsFile' argument.`);
        });

        it("when the dispatchFolder points to a nonexistent folder", async function () {
            const configuration = {
                dispatchFolder : resolve(__dirname, "mocks", "fail", "nonexistentDispatch"),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                logger: this.logger
            };

            this.refresher.configuration = configuration;
            await this.refresher.refreshSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:${
                EOL }Error: Path to the Dispatch folder (${configuration.dispatchFolder}) leads to a nonexistent folder.${
                EOL }Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        });

        it("when the path to dispatch.json file doesn't exist after the dispatch refresh execution", async function () {
            sandbox.replace(this.refresher.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithOneDispatch.json"),
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                logger: this.logger
            };

            this.refresher.configuration = configuration;
            await this.refresher.refreshSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:${
                EOL }Error: There was an error in the dispatch refresh command:${
                EOL }Command: dispatch refresh --dispatch ${ join(configuration.dispatchFolder, 'es-mx', 'filledes-mxDispatch.dispatch') } --dataFolder ${ join(configuration.dispatchFolder, 'es-mx') +
                EOL }Error: Path to filledes-mxDispatch.json (${ join(configuration.dispatchFolder, 'es-mx', 'filledes-mxDispatch.json') }) leads to a nonexistent file. This may be due to a problem with the 'dispatch refresh' command.`);
        });

        it("when the path to dispatch file doesn't exist", async function () {
            const configuration = {
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithNoDispatch.json"),
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                logger: this.logger
            };

            this.refresher.configuration = configuration;
            await this.refresher.refreshSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:${
                EOL }Error: Path to the nonExistenceen-usDispatch.dispatch file leads to a nonexistent file.`);
        });

        it("when the dispatch external calls fails", async function () {
            sandbox.replace(this.refresher.childProcessUtils, "execute", (command, args) => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const configuration = {
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                logger: this.logger
            };

            this.refresher.configuration = configuration;
            await this.refresher.refreshSkill(configuration);
            const errorList = this.logger.getError();
            const localePlaceholder = errorList[errorList.length - 1].includes('en-us') ? 'en-us' : 'es-es';

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:${
                EOL }Error: There was an error in the dispatch refresh command:${
                EOL }Command: dispatch refresh --dispatch ${ join(configuration.dispatchFolder, localePlaceholder, `filled${ localePlaceholder }Dispatch.dispatch`) } --dataFolder ${ join(configuration.dispatchFolder, localePlaceholder) +
                EOL }Error: Mocked function throws an Error`);
        });

        it("when the luis generate external calls fails", async function () {
            sandbox.replace(this.refresher, "executeDispatchRefresh", (dispatchName, executionModelByCulture) => {
                return Promise.resolve("Mocked function successfully");
            });
            sandbox.replace(this.refresher.childProcessUtils, "execute", (command, args) => {
                return Promise.reject(new Error("Mocked function throws an Error"));
            });
            const configuration = {
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                logger: this.logger
            };

            this.refresher.configuration = configuration;
            await this.refresher.refreshSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while refreshing any Skill from the Assistant:${
                EOL }Error: There was an error in the bf luis:generate:${configuration.lgLanguage} command:${
                EOL }Command: bf luis:generate:${configuration.lgLanguage} --in "${ join(configuration.dispatchFolder, 'en-us', 'filleden-usDispatch.json') }" --out "${configuration.lgOutFolder}" --className "DispatchLuis" --force${
                EOL }Error: Mocked function throws an Error`);
        });
    });

    describe("should show a successfully message", function() {
        it("when the refresh execution has finished successfully", async function () {
            sandbox.replace(this.refresher.childProcessUtils, "execute", (command, args) => {
                return Promise.resolve("Mocked function successfully");
            });
            const configuration = {
                dispatchFolder : resolve(__dirname, join("mocks", "success", "dispatch")),
                cognitiveModelsFile : resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                logger: this.logger
            };

            this.refresher.configuration = configuration;
            await this.refresher.refreshSkill(configuration);
            const successList = this.logger.getSuccess();

            strictEqual(successList[successList.length - 1], `Successfully refreshed Dispatch model`);
        });
    });
});
