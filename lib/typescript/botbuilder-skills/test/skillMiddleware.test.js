/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join } = require("path");
const {
    ConversationState,
    MemoryStorage,
    UserState } = require("botbuilder");  
const { TestAdapter } = require("botbuilder-core");
const { SkillMiddleware } = require(join("..", "lib", "skillMiddleware"));
const { SkillContext } = require(join("..", "lib", "skillContext"));
const storage = new MemoryStorage();
const userState = new UserState(storage);
const conversationState = new ConversationState(storage); 
const skillContextAccessor = userState.createProperty(SkillContext.name);
const dialogStateAccessor = conversationState.createProperty("DialogState");

// Test basic invocation of Skills that have slots configured and ensure the slots are filled as expected.
describe("skill middleware", function() {
    describe("skill middleware populates skill context", function() {
        it("send a skillBeginEvent and check that was populated by SkillMiddleware", async function(){
            const skillBeginEvent = require(join(__dirname, "mocks", "testData", "skillBeginEvent.json"));
            const skillContextData = new SkillContext();
            skillContextData.setObj("PARAM1", "TEST1");
            skillContextData.setObj("PARAM2", "TEST2");
            
            // Ensure we have a copy
            skillBeginEvent.value = skillContextData; 

            const testAdapter = new TestAdapter(async function(context) {
                // Validate that SkillContext has been populated by the SKillMiddleware correctly
                await validateSkillContextData(context, skillContextData);
            }).use(new SkillMiddleware(
                conversationState,
                skillContextAccessor,
                dialogStateAccessor)
            );
    
            await testAdapter.send(skillBeginEvent);
        });
    });

    describe("skill middleware populates skill context different data types", function() {
        it("send a skillBeginEvent and check that was populated by SkillMiddleware", async function(){
            const skillBeginEvent = require(join(__dirname, "mocks", "testData", "skillBeginEvent.json"));
            const skillContextData = new SkillContext();
            skillContextData.setObj("PARAM1", Date.now());
            skillContextData.setObj("PARAM2", 3);
            skillContextData.setObj("PARAM3", undefined);
            
            // Ensure we have a copy
            skillBeginEvent.value = skillContextData; 

            const testAdapter = new TestAdapter(async function(context) {
                // Validate that SkillContext has been populated by the SKillMiddleware correctly
                await validateSkillContextData(context, skillContextData);
            }).use(new SkillMiddleware(
                conversationState,
                skillContextAccessor,
                dialogStateAccessor)
            );

            await testAdapter.send(skillBeginEvent);
        });    
    });

    describe("skill middleware empty skill context", function() {
        it("send a skillBeginEvent and check that was populated by SkillMiddleware", async function(){
            const skillBeginEvent = require(join(__dirname, "mocks", "testData", "skillBeginEvent.json"));
            const skillContextData = new SkillContext();

            // Ensure we have a copy
            skillBeginEvent.value = skillContextData;

            const testAdapter = new TestAdapter(async function(context) {
                // Validate that SkillContext has been populated by the SKillMiddleware correctly
                await validateSkillContextData(context, skillContextData);
            }).use(new SkillMiddleware(
                conversationState,
                skillContextAccessor,
                dialogStateAccessor)
            );

            await testAdapter.send(skillBeginEvent);
        });    
    });

    describe("skill middleware null slot data", function() {
        it("send a skillBeginEvent and check that was populated by SkillMiddleware", async function(){
            const skillBeginEvent = require(join(__dirname, "mocks", "testData", "skillBeginEvent.json"));

            // Ensure we have a copy
            skillBeginEvent.value = undefined; 

            const testAdapter = new TestAdapter(async function(context) {
            }).use(new SkillMiddleware(
                conversationState,
                skillContextAccessor,
                dialogStateAccessor)
            );

            await testAdapter.send(skillBeginEvent);
        });    
    });

    describe("skill middleware null event name", function() {
        it("send a skillBeginEvent and check that was populated by SkillMiddleware", async function(){
            const skillBeginEvent = require(join(__dirname, "mocks", "testData", "skillBeginEvent.json"));
            
            // Ensure we have a copy
            skillBeginEvent.name = undefined; 

            const testAdapter = new TestAdapter(async function(context) {
            }).use(new SkillMiddleware(
                conversationState,
                skillContextAccessor,
                dialogStateAccessor)
            );

            await testAdapter.send(skillBeginEvent);
        });    
    });
});

async function validateSkillContextData(context, skillTestDataToValidate){
    const skillContext = await skillContextAccessor.get(context, new SkillContext());

    strictEqual(JSON.stringify(skillContext), JSON.stringify(skillTestDataToValidate));
}