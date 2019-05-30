/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
const assert = require("assert");
const { InputHints } = require("botframework-schema");
const { TestResponses } = require("./helpers/testResponses");
const { ResponseManager } = require("../lib/responses/responseManager");

describe("response generation", function() {
    
    describe("get response using generated accessor", function() {
        it("verify the values of the test responses using a response manager", function(){
            const responseManager = new ResponseManager(
                ["en", "es"],
                [TestResponses]
            );

            const response = responseManager.getResponseTemplate(TestResponses.getResponseText, "en");
            assert.deepStrictEqual(InputHints.ExpectingInput, response.inputHint);
            assert.deepStrictEqual("Suggestion 1", response.suggestedActions[0]);
            assert.deepStrictEqual("Suggestion 2", response.suggestedActions[1]);

            const reply = response.reply;
            assert.deepStrictEqual("The text", reply.text);
            assert.deepStrictEqual("The speak", reply.speak);
            assert.deepStrictEqual("The card text", reply.cardText);
        });
    });
});