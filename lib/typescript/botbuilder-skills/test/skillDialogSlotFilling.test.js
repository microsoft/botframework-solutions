/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join } = require("path");
const {
    ConversationState,
    MemoryStorage,
    NullTelemetryClient,
    UserState } = require("botbuilder");   
const skillDialogTestBase = require(join(__dirname, "helpers", "skillDialogTestBase"));
const { SkillDialogTest } = require(join(__dirname, "helpers", "skillDialogTest"));
const manifestUtilities = require(join(__dirname, "helpers", "manifestUtilities"));
const { MockMicrosoftAppCredentials } = require(join(__dirname, "mocks", "mockMicrosoftAppCredentials"));
const { MockSkillTransport } = require(join(__dirname, "mocks", "mockSkillTransport"));
const { SkillContext } = require(join("..", "lib", "skillContext"));
const storage = new MemoryStorage();
const userState = new UserState(storage);
const conversationState = new ConversationState(storage); 
const skillContextAccessor = userState.createProperty(SkillContext.name);
const dialogStateAccessor = conversationState.createProperty("DialogState");
const skillManifests = [];
const skillDialogTests = [];
const telemetryClient = new NullTelemetryClient();
const mockAppCredentials = new MockMicrosoftAppCredentials();
const mockSkillTransport = new MockSkillTransport();

// Test basic invocation of Skills that have slots configured and ensure the slots are filled as expected.
describe("skill dialog invocation", function() {
    before(async function() {
        // Simple skill, no slots
        skillManifests.push(manifestUtilities.createSkill(
            "testskill",
            "testskill",
            "https://testskill.tempuri.org/api/skill",
            "testSkill/testAction"));

        // Simple skill, with one slot (param1)
        const slots = [];
        slots.push({
            name: "param1",
            types: ["string"]
        });
        skillManifests.push(manifestUtilities.createSkill(
            "testskillwithslots",
            "testskillwithslots",
            "https://testskillwithslots.tempuri.org/api/skill",
            "testSkill/testActionWithSlots",
            slots));

        // Simple skill, with two actions and multiple slots
        const multiParamSlots = [];
        multiParamSlots.push({
            name: "param1",
            types: ["string"]
        });
        multiParamSlots.push({
            name: "param2",
            types: ["string"]
        });
        multiParamSlots.push({
            name: "param3",
            types: ["string"]
        });

        const multiActionSkill = manifestUtilities.createSkill(
            "testskillwithmultipleactionsandslots",
            "testskillwithmultipleactionsandslots",
            "https://testskillwithslots.tempuri.org/api/skill",
            "testSkill/testAction1",
            multiParamSlots);

        multiActionSkill.actions.push(manifestUtilities.createAction(
            "testSkill/testAction2",
            multiParamSlots
        ));
        skillManifests.push(multiActionSkill);

        // Each Skill has a number of actions, these actions are added as their own SkillDialog enabling
        // the SkillDialog to know which action is invoked and identify the slots as appropriate.
        skillManifests.forEach(skill => {
            skillDialogTests.push(
                new SkillDialogTest(
                    skill,
                    mockAppCredentials,
                    telemetryClient,
                    skillContextAccessor,
                    undefined,
                    mockSkillTransport
                )
            )
        });
    });

    beforeEach(async function() {
        await skillDialogTestBase.initialize(userState, conversationState, skillContextAccessor, dialogStateAccessor);
    });

    // Ensure the SkillBegin event activity is sent to the Skill when starting a skill conversation.
    describe("skill begin event", function() {
        it("send 'hello' and check if the activity was sent to the skill when starting a skill conversation", async function(){
            const eventToMatch = require(join(__dirname, "mocks", "testData", "skillBeginEvent.json"));
            
            const testAdapter = skillDialogTestBase.getTestAdapter(
                skillManifests.find(skillManifest => skillManifest.name === "testskill"),
                skillDialogTests,
                "testSkill/testAction",
                undefined);
            await testAdapter.send("hello");

            strictEqual(true, mockSkillTransport.verifyActivityForwardedCorrectly(eventToMatch));
        });
    });

    // Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
    // and present in State.
    describe("skill begin event with slots", function() {
        it("send 'hello' and check that skillBegin event was sent and included the slots that were configured in the manifest", async function(){
            const eventToMatch = require(join(__dirname, "mocks", "testData", "skillBeginEventWithOneParam.json"));
            
            // Data to add to the UserState managed SkillContext made available for slot filling
            // within SkillDialog
            const slot = { key: "param1", value: "TEST" };
            const slots = [];
            slots.push(slot);
            const testAdapter = skillDialogTestBase.getTestAdapter(
                skillManifests.find(skillManifest => skillManifest.name === "testskillwithslots"),
                skillDialogTests,
                "testSkill/testActionWithSlots",
                slots);
            await testAdapter.send("hello");
    
            strictEqual(true, mockSkillTransport.verifyActivityForwardedCorrectly(eventToMatch));
        });
    });

    // Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
    // This test has extra data in the SkillContext "memory" which should not be sent across
    // and present in State.
    describe("skill begin event with slots extra items", function() {
        it("send 'hello' and check that skillBegin event was sent and included the slots that were configured in the manifest", async function(){
            const eventToMatch = require(join(__dirname, "mocks", "testData", "skillBeginEventWithOneParam.json"));
            
            // Data to add to the UserState managed SkillContext made available for slot filling
            // within SkillDialog
            const slot1 = { key: "param1", value: "TEST" };
            const slot2 = { key: "param2", value: "TEST" };
            const slot3 = { key: "param3", value: "TEST" };
            const slot4 = { key: "param4", value: "TEST" };
            const slots = [];
            slots.push(slot1);
            slots.push(slot2);
            slots.push(slot3);
            slots.push(slot4);

            const testAdapter = skillDialogTestBase.getTestAdapter(
                skillManifests.find(skillManifest => skillManifest.name === "testskillwithslots"),
                skillDialogTests,
                "testSkill/testActionWithSlots",
                slots);
            await testAdapter.send("hello");
    
            strictEqual(true, mockSkillTransport.verifyActivityForwardedCorrectly(eventToMatch));
        });
    });

    // Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
    // and present in State. This doesn't pass an action so "global" slot filling is used
    describe("skill begin event no action passed", function() {
        it("send 'hello' and check that skillBegin event was sent and included the slots that were configured in the manifest and present in the state", async function(){
            const eventToMatch = require(join(__dirname, "mocks", "testData", "skillBeginEventWithTwoParams.json"));
    
            // Data to add to the UserState managed SkillContext made available for slot filling
            // within SkillDialog
            const slot1 = { key: "param1", value: "TEST" };
            const slot2 = { key: "param2", value: "TEST2" };
            const slots = [];
            slots.push(slot1);
            slots.push(slot2);
    
            // Not passing action to test the "global" slot filling behaviour
            const testAdapter = skillDialogTestBase.getTestAdapter(
                skillManifests.find(skillManifest => skillManifest.name === "testskillwithmultipleactionsandslots"),
                skillDialogTests,
                undefined,
                slots);
            await testAdapter.send("hello");
    
            strictEqual(true, mockSkillTransport.verifyActivityForwardedCorrectly(eventToMatch));
        });        
    });
});