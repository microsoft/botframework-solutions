/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join, resolve } = require("path");
const { writeFileSync, readFileSync } = require("fs");
const sandbox = require("sinon").createSandbox();
const testLogger = require("./helpers/testLogger");
const botskills = require("../lib/index");
const { getNormalizedFile } = require("./helpers/normalizeUtils");
const emptyAppsettings = getNormalizedFile(resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")));
const appsettingsWithTestSkill = getNormalizedFile(resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")));
const { EOL } = require('os');

function undoChangesInTemporalFiles() {
    writeFileSync(resolve(__dirname, join("mocks", "appsettings", "emptyAppsettings.json")), emptyAppsettings);
    writeFileSync(resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")), appsettingsWithTestSkill);
}

describe("The update command", function () {
    beforeEach(function () {
        undoChangesInTemporalFiles();
        this.logger = new testLogger.TestLogger();
        this.updater = new botskills.UpdateSkill();
        this.updater.logger = this.logger;
    });

    this.afterAll(function () {
        undoChangesInTemporalFiles();
    });
    
    describe("should show an error", function () {
        it("when the local skill to update is not present in the assistant manifest", async function() {
            const configuration = {
                botName: "mock-assistant",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "absentSkillManifest.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")),
                cognitiveModelsFile: resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "ts",
                logger: this.logger
            };

            this.updater.configuration = configuration;
            await this.updater.updateSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while updating the Skill from the Assistant:${
                EOL }Error: The Skill doesn't exist in the Assistant, run 'botskills connect --localManifest "${configuration.localManifest}" --luisFolder "${configuration.luisFolder}" --${configuration.lgLanguage}'`);
        });

        it("when the remote skill to update is not present in the assistant manifest", async function() {
            sandbox.replace(this.updater, "existSkill", () => {
                return Promise.resolve(false);
            })

            const configuration = {
                botName: "mock-assistant",
                localManifest: "",
                remoteManifest: resolve(__dirname, join("mocks", "manifests", "v2", "absentManifest.json")),
                languages: "",
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")),
                cognitiveModelsFile: resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "ts",
                logger: this.logger
            };

            this.updater.configuration = configuration;
            await this.updater.updateSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while updating the Skill from the Assistant:${
                EOL }Error: The Skill doesn't exist in the Assistant, run 'botskills connect --remoteManifest "${configuration.remoteManifest}" --luisFolder "${configuration.luisFolder}" --${configuration.lgLanguage}'`);
        });

        it("when the localManifest points to a nonexisting Skill manifest file", async function () {
            const configuration = {
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "nonexistentSkill.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")),
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: this.logger
            };

            this.updater.configuration = configuration;
            await this.updater.updateSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1], `There was an error while updating the Skill from the Assistant:${
                EOL }Error: The 'localManifest' argument leads to a non-existing file.${
                EOL }Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        });

        it("when the remoteManifest points to a nonexisting Skill manifest URL", async function() {
            const configuration = {
                botName: "",
                localManifest: "",
                remoteManifest: "http://nonexistentSkill.azurewebsites.net/api/skill/manifest",
                languages: "",
                luisFolder: "",
                dispatchFolder: "",
                outFolder: "",
                lgOutFolder: "",
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")),
                cognitiveModelsFile: "",
                lgLanguage: "",
                logger: this.logger
            };

            this.updater.configuration = configuration;
            await this.updater.updateSkill(configuration);
            const errorList = this.logger.getError();

            strictEqual(errorList[errorList.length - 1].includes('getaddrinfo ENOTFOUND'), true);
        });
    });

    describe("should show a success message", function () {
        it("when the skill is successfully updated to the Assistant", async function () {
            const configuration = {
                skillId: "testSkill",
                botName: "",
                localManifest: resolve(__dirname, join("mocks", "manifests", "v2", "manifest.json")),
                remoteManifest: "",
                languages: "",
                luisFolder: resolve(__dirname, join("mocks", "success", "luis")),
                dispatchFolder: resolve(__dirname, join("mocks", "success", "dispatch")),
                outFolder: "",
                lgOutFolder : resolve(__dirname, "mocks", "success", "luis"),
                resourceGroup: "",
                appSettingsFile: resolve(__dirname, join("mocks", "appsettings", "appsettingsWithTestSkill.json")),
                cognitiveModelsFile: resolve(__dirname, "mocks", "cognitivemodels", "cognitivemodelsWithTwoDispatch.json"),
                lgLanguage: "ts",
                logger: this.logger
            };

            this.updater.configuration = configuration;
            await this.updater.updateSkill(configuration);
            const successList = this.logger.getSuccess();

            strictEqual(successList[successList.length - 1], `Successfully updated '${configuration.skillId}' skill from your assistant's skills configuration file.`);
        });
    });
});
