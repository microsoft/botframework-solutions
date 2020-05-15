/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const { join } = require("path");
const { strictEqual } = require("assert");
const { InputHints } = require("botframework-schema");
const { TestResponses } = require(join(__dirname, "..", "helpers", "testResponses"));
const { ResponseManager } = require(join("..", "..", "lib", "responses", "responseManager"));

describe("response generation", function() {
    
    describe("get response using generated accessor", function() {
        it("verify the values of the test responses using a response manager", function(){
            const responseManager = new ResponseManager(
                ["en", "es"],
                [TestResponses]
            );

            const response = responseManager.getResponseTemplate(TestResponses.getResponseText, "en");
            strictEqual(InputHints.ExpectingInput, response.inputHint);
            strictEqual("Suggestion 1", response.suggestedActions[0]);
            strictEqual("Suggestion 2", response.suggestedActions[1]);

            const reply = response.reply;
            strictEqual("The text", reply.text);
            strictEqual("The speak", reply.speak);
            strictEqual("The card text", reply.cardText);
        });
    });
});