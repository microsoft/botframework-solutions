/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const { SkillRouter } = require("../lib/skillRouter");

describe("manifest", function() {
    before(async function() {
    });

    describe("desearialize valid manifest file", function() {
        it("verify valid manifest structure", async function(){
            assert.ok(require("./mocks/resources/manifestTemplate.json"));
        });        
    });

    describe("desearialize invalid manifest file", function() {
        it("verify invalid manifest structure", async function(){
            assert.throws(() => { require("./mocks/testData/malformedManifestTemplate.json") }, SyntaxError);
        });        
    });

    describe("is skill helper", function() {
        it("verify existence for events in the manifest", async function(){
            const skillManifest = require("./mocks/resources/manifestTemplate.json")
            const skillManifests = [];
            skillManifests.push(skillManifest);
            assert.ok(SkillRouter.isSkill(skillManifests, "calendarSkill/createEvent"));
            assert.ok(SkillRouter.isSkill(skillManifests, "calendarSkill/updateEvent"));
            assert.deepStrictEqual(undefined, SkillRouter.isSkill(skillManifests, "calendarSkill/MISSINGEVENT"));
        });
    });
})