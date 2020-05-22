/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { ok } = require("assert");
const { join } = require("path");
const { LocaleTemplateManager } = require(join("..", "..", "lib", "responses", "localeTemplateManager"));

let localeTemplateManager;

describe("language generation", function() {
    before(async function() {
        const localeLgFiles = new Map();
        localeLgFiles.set("en-us", join(__dirname, "..", "responses", "testResponses.lg"));
        localeLgFiles.set("es-es", join(__dirname, "..", "responses", "testResponses.es.lg"));
        
        localeTemplateManager = new LocaleTemplateManager(localeLgFiles, "en-us");
    });
    
    describe("get response with language generation english", function() {
        it("should return the correct response included in the possible responses of the locale", function() {
            let defaultCulture =  i18next.language;

            // Generate English response using LG with data
            let data = { Name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", 'en-us', data);

            // Retrieve possible responses directly from the correct template to validate logic
            let possibleResponses = localeTemplateManager.lgPerLocale.get('en-us').expandTemplate("HaveNameMessage", data);

            ok(possibleResponses.includes(response.text));

            i18next.language = defaultCulture;
        });
    });

    describe("get response with language generation spanish", function() {
        it("should return the correct response included in the possible responses of the locale", function() {
            let defaultCulture =  i18next.language;

            // Generate Spanish response using LG with data
            let data = { name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", 'es-es', data);

            // Retrieve possible responses directly from the correct template to validate logic
            const possibleResponses = localeTemplateManager.lgPerLocale.get('es-es').expandTemplate("HaveNameMessage", data);

            ok(possibleResponses.includes(response.text));

            i18next.language = defaultCulture;
        });
    });

    xdescribe("get response with language generation fallback", function() {
        // This test will remain commented until the fallback for Template Engine is implemented
        it("should return a list that contains the response text of the fallback language", function() {
            let defaultCulture =  i18next.language;

            // German locale not supported, locale template engine should fallback to english as per default in Test Setup.

            // Generate English response using LG with data
            let data = { name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", data);

            // Retrieve possible responses directly from the correct template to validate logic
            // Logic should fallback to english due to unsupported locale
            var possibleResponses = localeTemplateManager.templateEnginesPerLocale["en-us"].expandTemplate("HaveNameMessage", data);

            strictEqual(possibleResponses.includes(response.text));

            i18next.language = defaultCulture;
        });
    });
});
