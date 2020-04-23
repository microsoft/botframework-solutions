/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const { strictEqual } = require("assert");
const { join } = require("path");
const i18next = require("i18next").default;
const { ReadPreference, SpeechUtility } = require(join("..", "..", "lib", "responses", "speechUtility"));
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

describe("speech utility", function() {
    
    before(async function() {
        this.activity = {
            speak: parentSpeakProperty
        };
        this.promptOptions = {
            prompt: {
                text: parentSpeakProperty,
                speak: parentSpeakProperty
            }
        }
        this.andOperator = i18next.t("common:and"); 
    });

    describe("get speech ready string from one prompt option", function() {
        it("verify the speak response of one choice", function(){
            this.promptOptions.choices = [
                {
                    value: listItemSpeakProperty
                }
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.promptOptions);

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

            const response = SpeechUtility.listToSpeechReadyString(this.promptOptions, ReadPreference.Chronological)
            
            const item1 = i18next.t("common:latestItem").replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:lastItem").replace("{0}", listItemSpeakProperty);

            strictEqual(response, `${parentSpeakProperty} ${item1} ${this.andOperator} ${item2}`);
        });
    });

    describe("get speech ready string from activity with one attachment", function() {
        it("verify the speak response from activity with one attachment", function(){
            this.activity.attachments = [
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.activity);

            strictEqual(response, `${parentSpeakProperty} ${listItemSpeakProperty}`);
        });
    });

    describe("get speech ready string from activity with two attachment", function() {
        it("verify the speak response from activity with two attachment", function(){
            this.activity.attachments = [
                attachment,
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(this.activity);

            const item1 = i18next.t("common:firstItem")
                .replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:lastItem")
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

            const response = SpeechUtility.listToSpeechReadyString(this.activity);

            const item1 = i18next.t("common:firstItem")
                .replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:secondItem")
                .replace("{0}", listItemSpeakProperty);
            const item3 = i18next.t("common:lastItem")
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

            const response = SpeechUtility.listToSpeechReadyString(this.activity);
            
            const item1 = i18next.t("common:firstItem")
                .replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:secondItem")
                .replace("{0}", listItemSpeakProperty);
            const item3 = i18next.t("common:thirdItem")
                .replace("{0}", listItemSpeakProperty);
            const item4 = i18next.t("common:lastItem")
                .replace("{0}", listItemSpeakProperty);

            strictEqual(response, `${parentSpeakProperty} ${item1}, ${item2}, ${item3} ${this.andOperator} ${item4}`);
        });
    });
});