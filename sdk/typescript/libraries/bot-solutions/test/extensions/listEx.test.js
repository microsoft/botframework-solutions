/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join } = require("path");
const { readFileSync } = require("fs");
const { SomeComplexType } = require(join(__dirname, "..", "helpers", "someComplexType"));
const { ListEx } = require(join("..", "..", "lib", "extensions", "listEx"));
const { CommonResponses } = require(join("..", "..", "lib", "resources"));
const { ResponsesUtil } = require(join("..", "..", "lib", "util"));

const locale = 'en-us';

describe("list extensions", function() {
    
    before(async function() {
        const jsonPath = ResponsesUtil.getResourcePath(CommonResponses.name, CommonResponses.pathToResource, locale);
        this.commonFile = readFileSync(jsonPath, 'utf8');
        this.orOperator = JSON.parse(this.commonFile)["or"];
    });

    describe("defaults", function() {
        it("verify the speech string of multiple strings", function(){
            const andOperator = JSON.parse(this.commonFile)["and"];
            // Default is ToString and final separator is "and"
            const testList = ["One", "Two", "Three"];

            strictEqual("One, Two and Three", ListEx.toSpeechString(testList, andOperator, locale));
        });
    });

    describe("to speech string", function() {
        it("verify the speech string of multiple complex type objects", function(){
            const testList = [];

            strictEqual("", ListEx.toSpeechString(testList, this.orOperator, locale, (li) => { return li.number }));
            
            testList.push(new SomeComplexType("One", "Don't care"));
            strictEqual("One", ListEx.toSpeechString(testList, this.orOperator, locale, (li) => { return li.number }));

            testList.push(new SomeComplexType("Two", "Don't care"));
            strictEqual("One or Two", ListEx.toSpeechString(testList, this.orOperator, locale, (li) => { return li.number }));

            testList.push(new SomeComplexType("Three", "Don't care"));
            strictEqual("One, Two or Three", ListEx.toSpeechString(testList, this.orOperator, locale, (li) => { return li.number }));

            testList.push(new SomeComplexType("Four", "Don't care"));
            strictEqual("One, Two, Three or Four", ListEx.toSpeechString(testList, this.orOperator, locale, (li) => { return li.number }));
        });
    });
});