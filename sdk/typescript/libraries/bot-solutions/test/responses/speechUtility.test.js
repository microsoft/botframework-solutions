/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join } = require("path");
const { readFileSync } = require("fs");
const { ReadPreference, SpeechUtility } = require(join("..", "..", "lib", "responses", "speechUtility"));
const { CommonResponses } = require(join("..", "..", "lib", "resources"));
const { ResponsesUtil } = require(join("..", "..", "lib", "util"));
const parentSpeakProperty = "Parent speak property";
const listItemSpeakProperty = "List item speak property";
const version = {
    major: 1,
    minor: 2
}
const attachment = {
    contentType: "application/vnd.microsoft.card.adaptive",
    content: {
        version: version,
        speak: listItemSpeakProperty,
        type: "AdaptiveCard"
    }
}
const locale = 'en-us';

describe("speech utility", function() {
    
    before(async function() {
        const jsonPath = ResponsesUtil.getResourcePath(CommonResponses.name, CommonResponses.pathToResource, locale);
        this.commonFile = readFileSync(jsonPath, 'utf8');
        this.activity = {
            speak: parentSpeakProperty
        };
        this.promptOptions = {
            prompt: {
                text: parentSpeakProperty,
                speak: parentSpeakProperty
            }
        }
        this.andOperator = JSON.parse(this.commonFile)["and"]; 
    });

    describe("get speech ready string from one prompt option", function() {
        it("verify the speak response of one choice", function(){
            this.promptOptions.choices = [
                {
                    value: listItemSpeakProperty
                }
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.promptOptions, locale);

            strictEqual(response, `${parentSpeakProperty} ${listItemSpeakProperty}`);
        });
    });

    describe("get speech ready string from two prompt options chronological", function() {
        it("verify the speak response of multi choice", function(){
            this.promptOptions.choices = [
                {
                    value: listItemSpeakProperty
                },
                {
                    value: listItemSpeakProperty
                }
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.promptOptions, locale, ReadPreference.Chronological);
            
            const item1 = JSON.parse(this.commonFile)["latestItem"].replace("{0}", listItemSpeakProperty);
            const item2 = JSON.parse(this.commonFile)["lastItem"].replace("{0}", listItemSpeakProperty);

            strictEqual(response, `${parentSpeakProperty} ${item1} ${this.andOperator} ${item2}`);
        });
    });

    describe("get speech ready string from activity with one attachment", function() {
        it("verify the speak response from activity with one attachment", function(){
            this.activity.attachments = [
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.activity, locale);

            strictEqual(response, `${parentSpeakProperty} ${listItemSpeakProperty}`);
        });
    });

    describe("get speech ready string from activity with two attachment", function() {
        it("verify the speak response from activity with two attachment", function(){
            this.activity.attachments = [
                attachment,
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.activity, locale);

            const item1 = JSON.parse(this.commonFile)["firstItem"]
                .replace("{0}", listItemSpeakProperty);
            const item2 = JSON.parse(this.commonFile)["lastItem"]
                .replace("{0}", listItemSpeakProperty);

            strictEqual(response, `${parentSpeakProperty} ${item1} ${this.andOperator} ${item2}`);
        });
    });

    describe("get speech ready string from activity with three attachment", function() {
        it("verify the speak response from activity with three attachment", function(){
            this.activity.attachments = [
                attachment,
                attachment,
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.activity, locale);

            const item1 = JSON.parse(this.commonFile)["firstItem"]
                .replace("{0}", listItemSpeakProperty);
            const item2 = JSON.parse(this.commonFile)["secondItem"]
                .replace("{0}", listItemSpeakProperty);
            const item3 = JSON.parse(this.commonFile)["lastItem"]
                .replace("{0}", listItemSpeakProperty);

            strictEqual(response, `${parentSpeakProperty} ${item1}, ${item2} ${this.andOperator} ${item3}`);
        });
    });

    describe("get speech ready string from activity with four attachment", function() {
        it("verify the speak response from activity with four attachment", function(){
            this.activity.attachments = [
                attachment,
                attachment,
                attachment,
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.activity, locale);
            
            const item1 = JSON.parse(this.commonFile)["firstItem"]
                .replace("{0}", listItemSpeakProperty);
            const item2 = JSON.parse(this.commonFile)["secondItem"]
                .replace("{0}", listItemSpeakProperty);
            const item3 = JSON.parse(this.commonFile)["thirdItem"]
                .replace("{0}", listItemSpeakProperty);
            const item4 = JSON.parse(this.commonFile)["lastItem"]
                .replace("{0}", listItemSpeakProperty);

            strictEqual(response, `${parentSpeakProperty} ${item1}, ${item2}, ${item3} ${this.andOperator} ${item4}`);
        });
    });
});