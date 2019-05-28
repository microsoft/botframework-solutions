/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const assert = require("assert");
const i18next = require("i18next").default;
const {
    ReadPreference,
    SpeechUtility } = require("../lib/responses/speechUtility");
const parentSpeakProperty = "Parent speak property";
const listItemSpeakProperty = "List item speak property";
const attachment = {
    contentType: "application/vnd.microsoft.card.adaptive",
    content: {
        speak: listItemSpeakProperty,
        type: "AdaptiveCard"
    }
}
let activity;
let promptOptions;
let andOperator;

describe("speech utility", function() {
    
    before(async function() {
        activity = {
            speak: parentSpeakProperty
        };

        promptOptions = {
            prompt: {
                text: parentSpeakProperty,
                speak: parentSpeakProperty
            }
        }

        andOperator = i18next.t("common:and"); 
    });

    describe("get speech ready string from one prompt option", function() {
        it("verify the speak response of one choice", function(){
            promptOptions.choices = [
                {
                    value: listItemSpeakProperty
                }
            ];

            const response = SpeechUtility.listToSpeechReadyString(promptOptions);

            assert.deepEqual(response, `${parentSpeakProperty}${SpeechUtility.breakString}${listItemSpeakProperty}`);
        });
    });

    describe("get speech ready string from two prompt options chronological", function() {
        it("verify the speak response of multi choice", function(){
            promptOptions.choices = [
                {
                    value: listItemSpeakProperty
                },
                {
                    value: listItemSpeakProperty
                }
            ];

            const response = SpeechUtility.listToSpeechReadyString(promptOptions, ReadPreference.Chronological)
            
            const item1 = i18next.t("common:latestItem").replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:lastItem").replace("{0}", listItemSpeakProperty);

            assert.deepEqual(response, `${parentSpeakProperty}${SpeechUtility.breakString}${item1} ${andOperator} ${item2}`);
        });
    });

    describe("get speech ready string from activity with one attachment", function() {
        it("verify the speak response from activity with one attachment", function(){
            activity.attachments = [
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(activity);

            assert.deepEqual(response, `${parentSpeakProperty}${SpeechUtility.breakString}${listItemSpeakProperty}`);
        });
    });

    describe("get speech ready string from activity with two attachment", function() {
        it("verify the speak response from activity with two attachment", function(){
            activity.attachments = [
                attachment,
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(activity);

            const item1 = i18next.t("common:firstItem")
                .replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:lastItem")
                .replace("{0}", listItemSpeakProperty);

            assert.deepEqual(response, `${parentSpeakProperty}${SpeechUtility.breakString}${item1} ${andOperator} ${item2}`);
        });
    });

    describe("get speech ready string from activity with three attachment", function() {
        it("verify the speak response from activity with three attachment", function(){
            activity.attachments = [
                attachment,
                attachment,
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(activity);

            const item1 = i18next.t("common:firstItem")
                .replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:secondItem")
                .replace("{0}", listItemSpeakProperty);
            const item3 = i18next.t("common:lastItem")
                .replace("{0}", listItemSpeakProperty);

            assert.deepEqual(response, `${parentSpeakProperty}${SpeechUtility.breakString}${item1}, ${item2} ${andOperator} ${item3}`);
        });
    });

    describe("get speech ready string from activity with four attachment", function() {
        it("verify the speak response from activity with four attachment", function(){
            activity.attachments = [
                attachment,
                attachment,
                attachment,
                attachment
            ];

            const response = SpeechUtility.listToSpeechReadyString(activity);
            
            const item1 = i18next.t("common:firstItem")
                .replace("{0}", listItemSpeakProperty);
            const item2 = i18next.t("common:secondItem")
                .replace("{0}", listItemSpeakProperty);
            const item3 = i18next.t("common:thirdItem")
                .replace("{0}", listItemSpeakProperty);
            const item4 = i18next.t("common:lastItem")
                .replace("{0}", listItemSpeakProperty);

            assert.deepEqual(response, `${parentSpeakProperty}${SpeechUtility.breakString}${item1}, ${item2}, ${item3} ${andOperator} ${item4}`);
        });
    });
});