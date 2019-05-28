/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const i18next = require("i18next").default;
const { SomeComplexType } = require("./helpers/someComplexType");
const { ListExtensions } = require("../lib/extensions/listExtensions");
let orOperator;
describe("list extensions", function() {
    before(async function() {
        orOperator = i18next.t("common:or"); 
    });

    describe("defaults", function() {
        it("verify the speech string of multiple strings", function(){
            const andOperator = i18next.t("common:and"); 
            // Default is ToString and final separator is "and"
            const testList = ["One", "Two", "Three"];

            assert.deepEqual("One, Two and Three", ListExtensions.toSpeechString(testList, andOperator));
        });
    });

    describe("to speech string", function() {
        it("verify the speech string of multiple complex type objects", function(){
            const testList = [];

            assert.equal("", ListExtensions.toSpeechString(testList, orOperator, (li) => { return li.number }));
            
            testList.push(new SomeComplexType("One", "Don't care"));
            assert.equal("One", ListExtensions.toSpeechString(testList, orOperator, (li) => { return li.number }));

            testList.push(new SomeComplexType("Two", "Don't care"));
            assert.equal("One or Two", ListExtensions.toSpeechString(testList, orOperator, (li) => { return li.number }));

            testList.push(new SomeComplexType("Three", "Don't care"));
            assert.equal("One, Two or Three", ListExtensions.toSpeechString(testList, orOperator, (li) => { return li.number }));

            testList.push(new SomeComplexType("Four", "Don't care"));
            assert.equal("One, Two, Three or Four", ListExtensions.toSpeechString(testList, orOperator, (li) => { return li.number }));
        });
    });
});