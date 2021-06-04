/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { ok } = require("assert");
const { join } = require("path");
const { LocaleTemplateManager } = require(join("..", "..", "lib", "responses", "localeTemplateManager"));
const { Templates } = require("botbuilder-lg");

let localeTemplateManager;
let localeLgFiles;

describe("language generation", function() {
    before(async function() {
        localeLgFiles = new Map();
        localeLgFiles.set("en", join(__dirname, "..", "responses", "testResponses.lg"));
        localeLgFiles.set("es", join(__dirname, "..", "responses", "testResponses.es.lg"));
        
        localeTemplateManager = new LocaleTemplateManager(localeLgFiles, "en");
    });
    
    describe("get response with language generation english", function() {
        it("should return the correct response included in the possible responses of the locale", function() {
            // Generate English response using LG with data
            let data = { Name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", "en-us", data);

            // Retrieve possible responses directly from the correct template to validate logic
            const possibleResponses = Templates.parseFile(localeLgFiles.get("en")).expandTemplate("HaveNameMessage", data);

            ok(possibleResponses.includes(response.text));
        });
    });

    describe("get response with language generation spanish", function() {
        it("should return the correct response included in the possible responses of the locale", function() {
            // Generate Spanish response using LG with data
            let data = { name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", "es-es", data);

            // Retrieve possible responses directly from the correct template to validate logic
            const possibleResponses = Templates.parseFile(localeLgFiles.get("es")).expandTemplate("HaveNameMessage", data);

            ok(possibleResponses.includes(response.text));
        });
    });

    describe("get response with language generation fallback", function() {
        it("should return a list that contains the response text of the fallback language", function() {
            // German locale not supported, locale template engine should fallback to english as per default in Test Setup.

            // Generate English response using LG with data
            const data = { name: "Darren" };
            const response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", undefined, data);

            // Retrieve possible responses directly from the correct template to validate logic
            // Logic should fallback to english due to unsupported locale
            const possibleResponses = Templates.parseFile(localeLgFiles.get("en")).expandTemplate("HaveNameMessage", data);

            ok(possibleResponses.includes(response.text));
        });
    });
});
