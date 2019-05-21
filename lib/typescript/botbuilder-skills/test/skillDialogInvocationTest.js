/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const {
    ConversationState,
    MemoryStorage,
    NullTelemetryClient,
    UserState } = require("botbuilder");
const skillDialogTestBase = require("./helpers/skillDialogTestBase");
const { SkillDialogTest } = require("./helpers/skillDialogTest");
const manifestUtilities = require("./helpers/manifestUtilities");
const { MockMicrosoftAppCredentials } = require("./mocks/mockMicrosoftAppCredentials");
const { MockSkillTransport } = require("./mocks/mockSkillTransport");
const { SkillContext } = require("../lib/skillContext");
const telemetryClient = new NullTelemetryClient();
const storage = new MemoryStorage();
const userState = new UserState(storage);
const conversationState = new ConversationState(storage);
const skillContextAccessor = userState.createProperty(SkillContext.name);
const dialogStateAccessor = conversationState.createProperty("DialogState");
const mockAppCredentials = new MockMicrosoftAppCredentials();
const mockSkillTransport = new MockSkillTransport();
const skillDialogTests = [];
let skillManifest;

// Test basic invocation of Skills through the SkillDialog.
describe("skill dialog invocation", function() {
    before(async function() {
        // Simple skill, no slots
        skillManifest = manifestUtilities.createSkill(
            "testSkill",
            "testSkill",
            "https://testskill.tempuri.org/api/skill",
            "testSkill/testAction");

        // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
        skillDialogTests.push(new SkillDialogTest(
            skillManifest,
            mockAppCredentials,
            telemetryClient,
            skillContextAccessor,
            undefined,
            mockSkillTransport));
    })

    beforeEach(async function() {
        await skillDialogTestBase.initialize(userState, conversationState, skillContextAccessor, dialogStateAccessor);
    });

    // Create a SkillDialog and send a mesage triggering a call to the remote skill through the injected transport.
    // This ensures the SkillDialog is handling the SkillManifest and calling the skill correctly.
    describe("invoke skill dialog", function() {
        it("send 'hello' and check if the skill was invoked", async function(){
            const testAdapter = skillDialogTestBase.getTestAdapter(skillManifest, skillDialogTests, "testSkill/testAction", undefined);
            await testAdapter.send("hello");
            // Check if a request was sent to the mock, if not the test has failed (skill wasn't invoked).
            assert.deepEqual(true, mockSkillTransport.checkIfSkillInvoked());
        })
    });
});