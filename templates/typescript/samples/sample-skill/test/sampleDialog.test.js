/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const skillTestBase = require("./helpers/skillTestBase");
const testNock = require("./helpers/testBase");
const nameInput = "custom";

describe("sample dialog", function() {
    beforeEach(async function() {
        await skillTestBase.initialize();
    });

    describe("sample intent", function() {
        it("send 'sample dialog' and check you get the expected response", function(done) {
            const haveNameMessageTextResponse = skillTestBase.templateEngine.templateEnginesPerLocale.get('en-us').expandTemplate("NamePromptText");
            const namePromptTextResponse = skillTestBase.templateEngine.templateEnginesPerLocale.get('en-us').expandTemplate("HaveNameMessageText", { name: nameInput });
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send("sample dialog")
                .assertReplyOneOf(haveNameMessageTextResponse)
                .send(nameInput)
                .assertReplyOneOf(namePromptTextResponse);

            testNock.resolveWithMocks("sampleDialog_response", done, flow);
        });
    });
});
