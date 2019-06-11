/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const fs = require("fs");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");
let refreshSkillStub;

describe("The disconnect command", function () {
    afterEach(function (){
        sandbox.restore();
    });

    beforeEach(function () {
        this.logger = new testLogger.TestLogger();
        disconnector = new botskills.DisconnectSkill(this.logger);
        refreshSkillStub = sandbox.stub(disconnector.refreshSkill, "refreshSkill");
        refreshSkillStub.returns(Promise.resolve("Mocked function successfully"));
        fs.writeFileSync(resolve(__dirname, join("mocks", "resources", "filledDispatch.dispatch")),
            JSON.stringify(
                {
                    "services": [
                        {
                            "name": "testDispatch",
                            "id": "1"
                        }
                    ],
                    "serviceIds": [
                        "1"
                    ]
                }
                , null, 4));
        fs.writeFileSync(resolve(__dirname, join("mocks", "resources", "filledDispatchNoJson.dispatch")),
            JSON.stringify(
                {
                    "services": [
                        {
                            "name": "testDispatch",
                            "id": "1"
                        }
                    ],
                    "serviceIds": [
                        "1"
                    ]
                }
                , null, 4));
        fs.writeFileSync(resolve(__dirname, join("mocks", "resources", "filledSkillsArray.json")),
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
    })

	describe("should show an error", function () {
        it("when there is no skills File", async function () {
            const config = {
                skillId : "",
                skillsFile : "",
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder: resolve(__dirname, "..", "mocks", "resources"),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);
        });

        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "resources", "badSkillsArray.jso"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder: resolve(__dirname, ".." , "mocks", "resources"),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:
SyntaxError: Unexpected identifier`);
        });

        it("when the dispatchName and dispatchFolder point to a nonexistent file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "resources", "filledSkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "resources"),
                lgOutFolder: resolve(__dirname, "mocks", "resources"),
                dispatchName : "missingDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
			assert.strictEqual(ErrorList[ErrorList.length - 1], `Could not find file ${config.dispatchName}.dispatch. Please provide the 'dispatchName' and 'dispatchFolder' parameters.`);
        });

        it("when the refreshSkill fails", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "resources", "filledSkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "resources"),
                lgOutFolder: resolve(__dirname, "mocks", "resources"),
                dispatchName : "filledDispatchNoJson",
                lgLanguage : "cs",
                logger : this.logger
            };

            sandbox.replace(disconnector.refreshSkill, "refreshSkill", (command, args) => {
                return Promise.resolve(false);
            });
            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: There was an error while refreshing the Dispatch model.`);
        });

        it("when the lgOutFolder argument is invalid ", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "resources", "filledSkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "resources"),
                lgOutFolder : resolve(__dirname, "misleadingMockedFiles"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'lgOutFolder' argument is absent or leads to a non-existing folder.
Please make sure to provide a valid path to your LUISGen output folder.`);
        });

        it("when the lgLanguage argument is invalid", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "resources", "filledSkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "..", "mocks", "resources"),
                lgOutFolder : resolve(__dirname, "..", "mocks", "resources"),
                dispatchName : "filledDispatch",
                lgLanguage : "",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `The 'lgLanguage' argument is incorrect.
It should be either 'cs' or 'ts' depending on your assistant's language.`);
        });

        it("when the execution of a command fails", async function () {
            refreshSkillStub.returns(Promise.reject(new Error("Mocked function throws an Error")));
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "resources", "filledSkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "resources"),
                lgOutFolder : resolve(__dirname, "mocks", "resources"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const ErrorList = this.logger.getError();
            assert.strictEqual(ErrorList[ErrorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: Mocked function throws an Error`);
        });

    });

    describe("should show a warning", function () {
        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "resources", "emptySkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder: resolve(__dirname, "mocks", "resources"),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const WarningList = this.logger.getWarning();
            assert.strictEqual(WarningList[WarningList.length - 1], `The skill 'testSkill' is not present in the assistant Skills configuration file.
Run 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
        });

        it("when the dispatchName is not contained in the Dispatch file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "resources", "filledSkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "resources"),
                lgOutFolder: resolve(__dirname, "mocks", "resources"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const WarningList = this.logger.getWarning();
            assert.strictEqual(WarningList[WarningList.length - 1], `The skill ${config.skillId} is not present in the Dispatch model.
Run 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
		});
    });

    describe("should show a message", function () {
        
    });

    describe("should show a success message", function () {
        it("when the skill is successfully disconnected", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "resources", "filledSkillsArray.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "resources"),
                lgOutFolder : resolve(__dirname, "mocks", "resources"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await disconnector.disconnectSkill(config);
            const SuccessList = this.logger.getSuccess();
			assert.strictEqual(SuccessList[SuccessList.length - 1], `Successfully removed '${config.skillId}' skill from your assistant's skills configuration file.`);
        });
	});
});
