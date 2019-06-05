/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const { SkillRouter } = require("../lib/skillRouter");
const request = require("supertest");
const { server, botSettings } = require("./helpers/manifestServer");
const { withNock } = require("./helpers/nockHelper");
const nock = require("nock");
const { readFileSync } = require("fs");
const { join } = require("path");

describe("manifest", function() {
    describe("using restify server", function() {
        it("should generate the manifest without utterances", function(done) {
            const flow = request.agent(server.listen(3980))
            .get('/api/manifest')
            .expect(200)
            .expect(function(res) {
                assert.strictEqual(res.body.msAppId, botSettings.microsoftAppId, 'Skill Manifest msaAppId not set correctly');
                assert.strictEqual(res.body.endpoint, `http://${res.req.getHeaders().host}/api/skill/messages`, 'Skill Manifest endpoint not set correctly');
                assert.strictEqual(res.body.iconUrl, `http://${res.req.getHeaders().host}/calendarSkill.png`, 'Skill Manifest iconUrl not set correctly');
                assert.strictEqual(res.body.actions[0].definition.triggers.utteranceSources.length, 3);
                assert.strictEqual(res.body.actions[0].definition.triggers.utterances, undefined);
                server.close();
            });

            withNock('manifest_without_utterances', done, flow);
        });

        it("should generate the manifest with utterances", function(done) {
            const flow = request(server.listen(3980))
            .get('/api/manifest?inlineTriggerUtterances=true')
            .expect(200)
            .expect(function(res) {
                assert.strictEqual(res.body.msAppId, botSettings.microsoftAppId, 'Skill Manifest msaAppId not set correctly');
                assert.strictEqual(res.body.endpoint, `http://${res.req.getHeaders().host}/api/skill/messages`, 'Skill Manifest endpoint not set correctly');
                assert.strictEqual(res.body.iconUrl, `http://${res.req.getHeaders().host}/calendarSkill.png`, 'Skill Manifest iconUrl not set correctly');
                // Ensure each of the registered actions has triggering utterances added
                assert.strictEqual(res.body.actions.length, 7);
                res.body.actions.forEach(function (action) {
                    // If the trigger is an event we don't expect utterances
                    if (action.definition.triggers.events === undefined) {
                        assert.ok(action.definition.triggers.utterances[0].text.length > 0);
                        assert.ok(action.definition.triggers.utterances.find(u => u.locale === 'de'));
                        assert.ok(action.definition.triggers.utterances.find(u => u.locale === 'fr'));
                    }
                });
                server.close();
            });

            withNock('manifest_with_utterances', done, flow);
        });
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