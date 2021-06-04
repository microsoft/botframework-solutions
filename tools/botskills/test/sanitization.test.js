/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual, ok } = require("assert");
const { sanitizePath, sanitizeAppSettingsProperties } = require("../lib/utils");
const { join, sep } = require("path");

describe("The sanitization path util", function () {
    describe("should return a path without trailing backslash", function () {
        it("when a path does not contain a trailing backslash", async function() {
            const path = join('this', 'is', 'my', 'path');
            strictEqual(sanitizePath(path), path);
        });
        
        it("when a path contains a trailing backslash", async function() {
            const path = join('this', 'is', 'my', 'path', sep);
            strictEqual(sanitizePath(path), path.substring(0, path.length - 1));
        });
    })

    describe("should return an appSettingsFile with the BotFrameworkSkills property and its values with the first letter in lowercase", function () {
        it("when the first letter of the BotFrameworkSkills property and its values are in uppercase", async function() {
            const pathToAppSettingsFile = join(__dirname, "mocks", "appsettings", "appsettingsWithUppercase.json");
            const appSettings = JSON.parse(sanitizeAppSettingsProperties(pathToAppSettingsFile));
            ok('botFrameworkSkills' in appSettings);
            ok('id' in appSettings.botFrameworkSkills[0]);
            ok('name' in appSettings.botFrameworkSkills[0]);
            ok('appId' in appSettings.botFrameworkSkills[0]);
            ok('description' in appSettings.botFrameworkSkills[0]);
            ok('skillEndpoint' in appSettings.botFrameworkSkills[0]);
        });
    })
})