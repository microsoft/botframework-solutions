/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { writeFileSync } = require("fs");
const { join, resolve } = require("path");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");

describe("The disconnect command", function () {
    
    beforeEach(function () {
        writeFileSync(resolve(__dirname, join("mocks", "success", "dispatch", "filledDispatch.dispatch")),
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
        writeFileSync(resolve(__dirname, join("mocks", "success", "dispatch", "filledDispatchNoJson.dispatch")),
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
        this.disconnector = new botskills.DisconnectSkill(this.logger);
        this.refreshSkillStub = sandbox.stub(this.disconnector.refreshSkill, "refreshSkill");
        this.refreshSkillStub.returns(Promise.resolve("Mocked function successfully"));
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
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);
        });

        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "badSkills.jso"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:
SyntaxError: Unexpected token N in JSON at position 0`);
        });

        it("when the dispatchName and dispatchFolder point to a nonexistent file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "success", "nonexistentDispatch"),
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "missingDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const errorList = this.logger.getError();

			strictEqual(errorList[errorList.length - 1], `Could not find file ${config.dispatchName}.dispatch. Please provide the '--dispatchName' and '--dispatchFolder' arguments.`);
        });

        it("when the refreshSkill fails", async function () {
            sandbox.replace(this.disconnector.refreshSkill, "refreshSkill", (command, args) => {
                return Promise.resolve(false);
            });
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "success", "dispatch"),
                lgOutFolder: resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "filledDispatchNoJson",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: There was an error while refreshing the Dispatch model.`);
        });

        it("when the lgOutFolder argument is invalid ", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "success", "dispatch"),
                lgOutFolder: resolve(__dirname, "mocks", "success", "nonexistentLuis"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `The 'lgOutFolder' argument is absent or leads to a non-existing folder.
Please make sure to provide a valid path to your LUISGen output folder using the '--lgOutFolder' argument.`);
        });

        it("when the lgLanguage argument is invalid", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "success", "dispatch"),
                lgOutFolder : resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "filledDispatch",
                lgLanguage : "",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `The 'lgLanguage' argument is incorrect.
It should be either 'cs' or 'ts' depending on your assistant's language. Please provide either the argument '--cs' or '--ts'.`);
        });

        it("when the execution of a command fails", async function () {
            this.refreshSkillStub.returns(Promise.reject(new Error("Mocked function throws an Error")));
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "success", "dispatch"),
                lgOutFolder : resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while disconnecting the Skill ${config.skillId} from the Assistant:
Error: An error ocurred while updating the Dispatch model:
Error: Mocked function throws an Error`);
        });
    });

    describe("should show a warning", function () {
        it("when the skillsFile points to a bad formatted Assistant Skills configuration file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "emptySkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : "",
                lgOutFolder : resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const warningList = this.logger.getWarning();

            strictEqual(warningList[warningList.length - 1], `The skill 'testSkill' is not present in the assistant Skills configuration file.
Run 'botskills list --skillsFile "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
        });

        it("when the dispatchName is not contained in the Dispatch file", async function () {
            const config = {
                skillId : "testSkill",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "success", "dispatch"),
                lgOutFolder : resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const warningList = this.logger.getWarning();

            strictEqual(warningList[warningList.length - 1], `The skill ${config.skillId} is not present in the Dispatch model.
Run 'botskills list --skillsFile "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
		});
    });

    describe("should show a success message", function () {
        it("when the skill is successfully disconnected", async function () {
            const config = {
                skillId : "testDispatch",
                skillsFile: resolve(__dirname, "mocks", "virtualAssistant", "filledSkills.json"),
                outFolder : "",
                cognitiveModelsFile : "",
                language : "",
                luisFolder : "",
                dispatchFolder : resolve(__dirname, "mocks", "success", "dispatch"),
                lgOutFolder : resolve(__dirname, "mocks", "success", "luis"),
                dispatchName : "filledDispatch",
                lgLanguage : "cs",
                logger : this.logger
            };

            await this.disconnector.disconnectSkill(config);
            const successList = this.logger.getSuccess();

			strictEqual(successList[successList.length - 1], `Successfully removed '${config.skillId}' skill from your assistant's skills configuration file.`);
        });
	});
});
