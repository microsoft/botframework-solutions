/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License
 */

const assert = require('assert');
const { MemoryStorage } = require('botbuilder-core')
const { getTestAdapterDefault, templateEngine } = require('./helpers/botTestBase');
const testNock = require('./helpers/testBase');
let testStorage = new MemoryStorage();

describe("Onboarding Dialog", function () {
    describe("Onboarding", function () {
        beforeEach(function(done) {
            testStorage = new MemoryStorage();
            done();
        });

        it("start onboarding dialog sending a conversationUpdate", function (done) {
            const testName = 'Jane Doe';

            const profileState = { name: testName };

            const allNamePromptVariations = templateEngine.templateEnginesPerLocale.get("en-us").expandTemplate("NamePrompt");
            const allHaveMessageVariations = templateEngine.templateEnginesPerLocale.get("en-us").expandTemplate("HaveNameMessage", profileState);

            getTestAdapterDefault({ storage: testStorage }).then((testAdapter) => {
                const flow = testAdapter
                    .send({
                        type: "conversationUpdate",
                        membersAdded: [
                            {
                                id: "1",
                                name: "user"
                            }
                        ],
                    })
                    .assertReply((activity, description) => {
                        assert.strictEqual(1, activity.attachments.length)
                    })
                    .assertReplyOneOf(allNamePromptVariations)
                    .send(testName)
                    .assertReplyOneOf(allHaveMessageVariations)

                return testNock.resolveWithMocks('onboardingDialog_init', done, flow);
            });
        });    
    });
});   
