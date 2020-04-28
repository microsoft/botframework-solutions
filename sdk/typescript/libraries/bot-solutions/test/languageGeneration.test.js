/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { ok } = require("assert");
const { join } = require("path");
const i18next = require("i18next").default;
const { LocaleTemplateManager } = require(join("..", "lib", "responses", "localeTemplateManager"));

let localeTemplateManager;

describe("language generation", function() {
    after(async function() {
        i18next.changeLanguage('en-us');
    });

    before(async function() {
        const localeLgFiles = new Map();
        localeLgFiles.set("en-us", join(__dirname, "responses", "testResponses.lg"));
        localeLgFiles.set("es-es", join(__dirname, "responses", "testResponses.es.lg"));
        
        localeTemplateManager = new LocaleTemplateManager(localeLgFiles, "en-us");
    });
    
    describe("get response with language generation english", function() {
        it("should return the correct response included in the possible responses of the locale", function() {
            i18next.changeLanguage("en-us");

            // Generate English response using LG with data
            let data = { Name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", data);

            // Retrieve possible responses directly from the correct template to validate logic
            let possibleResponses = localeTemplateManager.lgPerLocale.get('en-us').expandTemplate("HaveNameMessage", data);

            ok(possibleResponses.includes(response.text));
        });
    });

    describe("get response with language generation spanish", function() {
        it("should return the correct response included in the possible responses of the locale", function() {
            i18next.changeLanguage("es-es");

            // Generate Spanish response using LG with data
            let data = { name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", data);

            // Retrieve possible responses directly from the correct template to validate logic
            var possibleResponses = localeTemplateManager.lgPerLocale.get('es-es').expandTemplate("HaveNameMessage", data);

            ok(possibleResponses.includes(response.text));
        });
    });

    xdescribe("get response with language generation fallback", function() {
        // This test will remain commented until the fallback for Template Engine is implemented
        it("should return a list that contains the response text of the fallback language", function() {
            // German locale not supported, locale template engine should fallback to english as per default in Test Setup.
            i18next.changeLanguage("de-de");

            // Generate English response using LG with data
            let data = { name: "Darren" };
            let response = localeTemplateManager.generateActivityForLocale("HaveNameMessage", data);

            // Retrieve possible responses directly from the correct template to validate logic
            // Logic should fallback to english due to unsupported locale
            var possibleResponses = localeTemplateManager.templateEnginesPerLocale["en-us"].expandTemplate("HaveNameMessage", data);

            strictEqual(possibleResponses.includes(response.text));
        });
    });
});
