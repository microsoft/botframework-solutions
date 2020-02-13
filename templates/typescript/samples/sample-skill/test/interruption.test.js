/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const skillTestBase = require("./helpers/skillTestBase");
const testNock = require("./helpers/testBase");

describe("interruption", function() {
    beforeEach(async function() {
        await skillTestBase.initialize();
    });

    describe("help interruption", function() {
        it("send 'help' during the sample dialog", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send("sample dialog")
                .assertReply(skillTestBase.getTemplates('NamePromptText'))
                .send("help")
                .assertReply("[Enter your help message here]");
            testNock.resolveWithMocks("interruption_help_response", done, flow);
        });
    });

    describe("cancel interruption", function() {
        it("send 'cancel' during the sample dialog", function(done) {
            const testAdapter = skillTestBase.getTestAdapter();
            const flow = testAdapter
                .send("sample dialog")
                .assertReply(skillTestBase.getTemplates('NamePromptText'))
                .send("cancel")
                .assertReply(skillTestBase.getTemplates('CancelledText'));
            testNock.resolveWithMocks("interruption_cancel_response", done, flow);
        });
    });
});
