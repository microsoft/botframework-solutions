// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class SpeechUtilityTests
    {
        private Activity _activity;

        private PromptOptions _promptOptions;

        private string parentSpeakProperty = "Parent speak property";

        private string listItemSpeakProperty = "List item speak property";

        private AdaptiveSchemaVersion adaptiveSchemaVersion;

        [TestInitialize]
        public void Setup()
        {
            _activity = new Activity() { Speak = parentSpeakProperty };
            _promptOptions = new PromptOptions() { Prompt = new Activity() { Text = parentSpeakProperty, Speak = parentSpeakProperty } };

            adaptiveSchemaVersion = new AdaptiveSchemaVersion(1, 2);
        }

        [TestMethod]
        public void GetSpeechReadyStringFromOnePromptOption()
        {
            _promptOptions.Choices = new List<Choice>()
            {
                new Choice(listItemSpeakProperty),
            };

            var response = SpeechUtility.ListToSpeechReadyString(_promptOptions);

            Assert.AreEqual(response, string.Format($"{parentSpeakProperty} {listItemSpeakProperty}"));
        }

        [TestMethod]
        public void GetSpeechReadyStringFromTwoPromptOptionsChronological()
        {
            _promptOptions.Choices = new List<Choice>()
            {
                new Choice(listItemSpeakProperty),
                new Choice(listItemSpeakProperty),
            };

            var response = SpeechUtility.ListToSpeechReadyString(_promptOptions, ReadPreference.Chronological);

            var item1 = string.Format(CommonStrings.LatestItem, listItemSpeakProperty);
            var item2 = string.Format(CommonStrings.LastItem, listItemSpeakProperty);
            Assert.AreEqual(response, string.Format($"{parentSpeakProperty} {item1} {CommonStrings.And} {item2}"));
        }

        [TestMethod]
        public void GetSpeechReadyStringFromActivityWithOneAttachment()
        {
            _activity.Attachments = new List<Attachment>
            {
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
            };

            var response = SpeechUtility.ListToSpeechReadyString(_activity);

            Assert.AreEqual(response, string.Format($"{parentSpeakProperty} {listItemSpeakProperty}"));
        }

        [TestMethod]
        public void GetSpeechReadyStringFromActivityWithTwoAttachments()
        {
            _activity.Attachments = new List<Attachment>
            {
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
            };

            var response = SpeechUtility.ListToSpeechReadyString(_activity);

            var item1 = string.Format(CommonStrings.FirstItem, listItemSpeakProperty);
            var item2 = string.Format(CommonStrings.LastItem, listItemSpeakProperty);
            Assert.AreEqual(response, string.Format($"{parentSpeakProperty} {item1} {CommonStrings.And} {item2}"));
        }

        [TestMethod]
        public void GetSpeechReadyStringFromActivityWithThreeAttachments()
        {
            _activity.Attachments = new List<Attachment>
            {
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
            };

            var response = SpeechUtility.ListToSpeechReadyString(_activity);

            var item1 = string.Format(CommonStrings.FirstItem, listItemSpeakProperty);
            var item2 = string.Format(CommonStrings.SecondItem, listItemSpeakProperty);
            var item3 = string.Format(CommonStrings.LastItem, listItemSpeakProperty);
            Assert.AreEqual(response, string.Format($"{parentSpeakProperty} {item1}, {item2} {CommonStrings.And} {item3}"));
        }

        [TestMethod]
        public void GetSpeechReadyStringFromActivityWithFourAttachments()
        {
            _activity.Attachments = new List<Attachment>
            {
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
                new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard(adaptiveSchemaVersion) { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }),
            };

            var response = SpeechUtility.ListToSpeechReadyString(_activity);

            var item1 = string.Format(CommonStrings.FirstItem, listItemSpeakProperty);
            var item2 = string.Format(CommonStrings.SecondItem, listItemSpeakProperty);
            var item3 = string.Format(CommonStrings.ThirdItem, listItemSpeakProperty);
            var item4 = string.Format(CommonStrings.LastItem, listItemSpeakProperty);
            Assert.AreEqual(response, string.Format($"{parentSpeakProperty} {item1}, {item2}, {item3} {CommonStrings.And} {item4}"));
        }
    }
}