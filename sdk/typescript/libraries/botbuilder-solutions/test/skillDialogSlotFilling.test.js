/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { 
    AutoSaveStateMiddleware,
    ConversationState,
    MemoryStorage,
    NullTelemetryClient,
    TestAdapter,
    TestFlow,
    UserState
} = require("botbuilder");
const { DialogSet } = require("botbuilder-dialogs");
const { EventDebuggerMiddleware } = require("../lib/middleware/eventDebuggerMiddleware");
const { SkillContext } = require("../lib");

const manifestUtilities = require("./helpers/manifestUtilities");
const { SkillDialogTest } = require("./helpers/skillDialogTest");
const { MockMicrosoftAppCredentials } = require("./mocks/mockMicrosoftAppCredentials");
const { MockSkillTransport } = require("./mocks/mockSkillTransport");

const storage = new MemoryStorage();
const userState = new UserState(storage);
const conversationState = new ConversationState(storage); 

const skillContextAccessor = userState.createProperty("SkillContext");
const dialogStateAccessor = conversationState.createProperty("DialogState");
const dialogs = new DialogSet(dialogStateAccessor);
const skillManifests = [];

const telemetryClient = new NullTelemetryClient();
const mockAppCredentials = new MockMicrosoftAppCredentials();
const mockSkillTransport = new MockSkillTransport();

/**
 * 
 * @param {(context) => Promise<void>} logic 
 * @returns {TestAdapter} test adapter
 */
function getTestAdapter(logic) {
    const adapter = new TestAdapter(logic)
        .use(new EventDebuggerMiddleware())
        .use(new AutoSaveStateMiddleware(userState, conversationState));

    return adapter;
}

/**
 * 
 * @param {{id: string}} skillManifest 
 * @param {string} actionId 
 * @param {SkillContext} slots 
 * @returns {TestFlow} test flow
 */
function getTestFlow(skillManifest, actionId, slots) {
    const flow = getTestAdapter(async function(context) {
        const dc = await dialogs.createContext(context);
        const sc = await skillContextAccessor.get(dc.context, new SkillContext());
        const skillContext = new SkillContext(sc.contextStorage);

        // If we have SkillContext data to populate
        if (slots) {
            // Add state to the SKillContext
            slots.forEachObj((value, key) => {
                skillContext.setObj(key, value);
            });
        }

        if (dc.activeDialog !== undefined) {
            await dc.continueDialog();
        } else {
            // ActionID lets the SkillDialog know which action to call
            await dc.beginDialog(skillManifest.id, actionId);
            // We don't continue as we don't care about the message being sent
            // just the initial instantiation, we need to send a message within tests
            // to invoke the flow. If continue is called then HttpMocks need be updated
            // to handle the subsequent activity "ack"
            // var result = await dc.continueDialog();
        }
    });

    return flow;
}

describe("Skill dialog slot filling", function() {
    before(function(done) {
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
            dialogs.add(
                new SkillDialogTest(
                    skill,
                    mockAppCredentials,
                    telemetryClient,
                    skillContextAccessor,
                    mockSkillTransport
                )
            )
        });

        done();
    });

    it("should get the params from semanticAction", async function() {
        const slots = new SkillContext();
        slots.setObj('param1', { key1: 'TEST1', key2: 'TEST2' });

        await getTestFlow(skillManifests.find(s => s.name === 'testskillwithslots'), 'testSkill/testActionWithSlots', slots)
        .send('hello')
        .startTest();

        mockSkillTransport.verifyActivityForwardedCorrectly(function(activity) {
            const semanticAction = activity.semanticAction;
            strictEqual(semanticAction.entities['param1'].properties['key1'], 'TEST1');
            strictEqual(semanticAction.entities['param1'].properties['key2'], 'TEST2');
        });
    });

    it("should get the params from semanticAction when the actionId is missing", async function() {
        const slots = new SkillContext();
        slots.setObj('param1', { key1: 'TEST1', key2: 'TEST2' });

        await getTestFlow(skillManifests.find(s => s.name === 'testskillwithmultipleactionsandslots'), undefined, slots)
        .send('hello')
        .startTest();

        mockSkillTransport.verifyActivityForwardedCorrectly(function(activity) {
            const semanticAction = activity.semanticAction;
            strictEqual(semanticAction.entities['param1'].properties['key1'], 'TEST1');
            strictEqual(semanticAction.entities['param1'].properties['key2'], 'TEST2');
        });
    });
});
