/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const { join, resolve } = require("path");
const sandbox = require('sinon').createSandbox();
const testLogger = require("../models/testLogger");
const botskills = require("../../lib/index");
let childProcessStub;

describe("The train command", function () {
    afterEach(function (){
        sandbox.restore();
    });

    beforeEach(function () {
        this.logger = new testLogger.TestLogger();
        this.trainer = new botskills.TrainSkill(this.logger);
        
        childProcessStub = sandbox.stub(this.trainer.childProcessUtils, "execute");
        childProcessStub.returns(Promise.resolve("Mocked function successfully"));
    });

    describe("should show a successfully message", function() {
        it("when the dispatchName and dispatchFolder point to the mocked file", async function () {
            const config = {
                dispatchName : "filledDispatch",
                dispatchFolder : resolve(__dirname, "../mockedFiles"),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "../mockedFiles"),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.trainer.trainSkill(config);
            const successList = this.logger.getSuccess();
            assert.strictEqual(true, successList.includes(`Successfully trained Dispatch model`));
        });
    });

    describe("should show an error message", function() {
        it("when the dispatchName and dispatchFolder point to a nonexistent file", async function () {
            const config = {
                dispatchName : "filledDispatchNoJson",
                dispatchFolder : resolve(__dirname, "../mockedFiles"),
                language: "ts",
                luisFolder: "",
                lgLanguage: "cs",
                outFolder: "",
                lgOutFolder: resolve(__dirname, "../mockedFiles"),
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.trainer.trainSkill(config);
            const errorList = this.logger.getError();
            assert.strictEqual(true, errorList.includes(`There was an error in the dispatch refresh command:\nError: Path to ${config.dispatchName}.json (${join(config.dispatchFolder, config.dispatchName)}.json) leads to a nonexistent file. Make sure the dispatch refresh command is being executed successfully`));
        });

        it("when the external calls fails", async function () {
            const updateDispatchStub = sandbox.stub(this.trainer, "updateDispatch");

            updateDispatchStub.returns(Promise.resolve("Mocked function successfully"))
            childProcessStub.returns(Promise.reject(new Error("Mocked function throws an Error")));

            const config = {
                dispatchName:  "filledDispatch",
                dispatchFolder : resolve(__dirname, "../mockedFiles"),
                language: "ts",
                luisFolder: "",
                lgLanguage: "",
                outFolder: "",
                lgOutFolder: "",
                cognitiveModelsFile: "",
                logger: this.logger
            };

            await this.trainer.trainSkill(config);
            const errorList = this.logger.getError();
            assert.strictEqual(true, errorList.includes(`There was an error in the luisgen command:\nError: Mocked function throws an Error`));
        });
    });
});


