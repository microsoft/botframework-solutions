/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join } = require("path");
const i18next = require("i18next").default;
const { SomeComplexType } = require(join(__dirname, "helpers", "someComplexType"));
const { ListEx } = require(join("..", "lib", "extensions", "listEx"));

describe("list extensions", function() {
    
    before(async function() {
        this.orOperator = i18next.t("common:or"); 
    });

    describe("defaults", function() {
        it("verify the speech string of multiple strings", function(){
            i18next.changeLanguage('en-us');
            const andOperator = i18next.t("common:and"); 
            // Default is ToString and final separator is "and"
            const testList = ["One", "Two", "Three"];

            strictEqual("One, Two and Three", ListEx.toSpeechString(testList, andOperator));
        });
    });

    describe("to speech string", function() {
        it("verify the speech string of multiple complex type objects", function(){
            i18next.changeLanguage('en-us');
            const testList = [];

            strictEqual("", ListEx.toSpeechString(testList, this.orOperator, (li) => { return li.number }));
            
            testList.push(new SomeComplexType("One", "Don't care"));
            strictEqual("One", ListEx.toSpeechString(testList, this.orOperator, (li) => { return li.number }));

            testList.push(new SomeComplexType("Two", "Don't care"));
            strictEqual("One or Two", ListEx.toSpeechString(testList, this.orOperator, (li) => { return li.number }));

            testList.push(new SomeComplexType("Three", "Don't care"));
            strictEqual("One, Two or Three", ListEx.toSpeechString(testList, this.orOperator, (li) => { return li.number }));

            testList.push(new SomeComplexType("Four", "Don't care"));
            strictEqual("One, Two, Three or Four", ListEx.toSpeechString(testList, this.orOperator, (li) => { return li.number }));
        });
    });
});