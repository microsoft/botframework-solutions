using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Builder.Solutions.Tests
{
    [TestClass]
    public class SpeechUtilityTests
    {

        private Activity _activity;

        private PromptOptions _promptOptions;

        private string parentSpeakProperty = "Parent speak property";

        private string listItemSpeakProperty ="List item speak property";

        [TestInitialize]
        public void Setup()
        {            
            _activity = new Activity() { Speak = parentSpeakProperty };
            _promptOptions = new PromptOptions() { Prompt = new Activity() { Speak = parentSpeakProperty } };
        }

        [TestMethod]
        public void GetSpeechReadyStringFromPromptOptions()
        {

        }

        [TestMethod]
        public void GetSpeechReadyStringFromActivityWithAttachments()
        {
            _activity.Attachments = new List<Attachment> { new Attachment(contentType: AdaptiveCard.ContentType, content: new AdaptiveCard() { Speak = listItemSpeakProperty, Type = AdaptiveCard.TypeName }) };
            var response = SpeechUtility.ListToSpeechReadyString(_activity);

            Assert.AreEqual(response, string.Format($"{parentSpeakProperty}<break/>{listItemSpeakProperty}"));
        }
    }
}
