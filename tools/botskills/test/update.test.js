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

describe("The update command", function () {
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
                },
                null, 4));
                
        this.logger = new testLogger.TestLogger();
        this.updater = new botskills.UpdateSkill(this.logger);
    });
    
    describe("should show an error", function () {
        it("when the skill to update is not present in the assistant manifest", async function() {
            const config = {
                botName: "mock-assistant",
                localManifest: resolve(__dirname, join("mocks", "skills", "absentManifest.json")),
                remoteManifest: "",
                dispatchName: "",
                language: "",
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile: "",
                lgLanguage: "ts",
                logger: this.logger
            };

            await this.updater.updateSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while updating the Skill from the Assistant:
Error: The Skill doesn't exist in the Assistant, run 'botskills connect --botName ${config.botName} --localManifest "${config.localManifest}" --luisFolder "${config.luisFolder}" --${config.lgLanguage}'`);
        });

        it("when the localManifest points to a nonexisting Skill manifest file", async function () {
            const config = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "nonexistentSkill.json")),
                remoteManifest: "",
                dispatchName: "",
                language: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: this.logger
            };

            await this.updater.updateSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while updating the Skill from the Assistant:
Error: The 'localManifest' argument leads to a non-existing file.
Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        });

        it("when the remoteManifest points to a nonexisting Skill manifest URL", async function() {
            const config = {
                botName: "",
                localManifest: "",
                remoteManifest: "http://nonexistentSkill.azurewebsites.net/api/skill/manifest",
                dispatchName: "",
                language: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                skillsFile: "",
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: this.logger
            };

            await this.updater.updateSkill(config);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while updating the Skill from the Assistant:
RequestError: Error: getaddrinfo ENOTFOUND nonexistentskill.azurewebsites.net nonexistentskill.azurewebsites.net:80`);            
        });
    });

    describe("should show a success message", function () {
        it("when the skill is successfully updated to the Assistant", async function () {
            sandbox.replace(this.updater.disconnectSkill, "disconnectSkill", () => {
                return Promise.resolve("Mocked function successfully");
            })
            sandbox.replace(this.updater.connectSkill, "connectSkill", () => {
                return Promise.resolve("Mocked function successfully");
            })
            const config = {
                skillId: "testSkill",
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "skills", "repeatedManifest.json")),
                remoteManifest: "",
                dispatchName: "testSkill",
                language: "",
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder: "",
                skillsFile: resolve(__dirname, join("mocks", "virtualAssistant", "filledSkills.json")),
                resourceGroup: "",
                appSettingsFile: "",
                cognitiveModelsFile: "",
                lgLanguage: "ts",
                logger: this.logger
            };

            await this.updater.updateSkill(config);
            const successList = this.logger.getSuccess();

            strictEqual(successList[successList.length - 1], `Successfully updated '${config.skillId}' skill from your assistant's skills configuration file.`);
        });
    });
});
