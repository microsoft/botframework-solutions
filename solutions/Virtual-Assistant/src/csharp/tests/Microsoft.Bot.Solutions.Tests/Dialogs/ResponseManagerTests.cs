using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Tests.Dialogs
{
    [TestClass]
    public class ResponseManagerTests
    {
        private string _resourceDir;
        private CultureInfo _currentCulture;

        [TestInitialize]
        public void Initialize()
        {
            _currentCulture = CultureInfo.CurrentUICulture;
            _resourceDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Dialogs");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // restore culture in case we changed it.
            CultureInfo.CurrentUICulture = _currentCulture;

        }

        [TestMethod]
        public void ReturnedResponseIsClone()
        {
            var rm = new ResponseManager(_resourceDir, "TestResponses");
            var copy1 = rm.GetBotResponse("GetResponseText");
            var copy2 = rm.GetBotResponse("GetResponseText");
            Assert.AreEqual(copy1.Replies[0].Text, copy2.Replies[0].Text);
            
            copy2.Replies[0].Text = "Something different";
            Assert.AreNotEqual(copy1.Replies[0].Text, copy2.Replies[0].Text);
        }

        [TestMethod]
        public void JsonNotFoundThrowsException()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            Assert.ThrowsException<FileNotFoundException>(() =>
            {
                var rm = new ResponseManager(_resourceDir, "NotThere");
                rm.GetBotResponse("Test");
            });
        }

        [TestMethod]
        public void KeyNotFoundThrowsException()
        {
            Assert.ThrowsException<KeyNotFoundException>(() =>
            {
                var rm = new ResponseManager(_resourceDir, "TestResponses");
                rm.GetBotResponse("NotThere");
            });
        }

        [TestMethod]
        public void MalFormedJsonThrowsException()
        {
            Assert.ThrowsException<JsonSerializationException>(() =>
            {
                var rm = new ResponseManager(_resourceDir, "BrokenJson");
                rm.GetBotResponse("GetResponseText");
            });
        }

        [TestMethod]
        public void InputHintDefaultsToAcceptingInput()
        {
            var rm = new ResponseManager(_resourceDir, "TestResponses");
            var response = rm.GetBotResponse("NoInputHint");
            Assert.AreEqual(InputHints.AcceptingInput, response.InputHint);
        }

        [TestMethod]
        public void LanguageFallback()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("es");
            var rm = new ResponseManager(_resourceDir, "TestResponses");
            var response = rm.GetBotResponse("GetResponseText");
            Assert.AreEqual("El texto", response.Reply.Text);

            response = rm.GetBotResponse("EnglishOnly");
            Assert.AreEqual("This wasn't found in spanish so the fallback answer is returned", response.Reply.Text);

        }

        [TestMethod]
        public void MultiLanguage()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var rm = new ResponseManager(_resourceDir, "TestResponses");
            var response = rm.GetBotResponse("MultiLanguage");
            Assert.AreEqual("This is in English", response.Reply.Text);

            CultureInfo.CurrentUICulture = new CultureInfo("es-MX");
            response = rm.GetBotResponse("MultiLanguage");
            Assert.AreEqual("Esto sería en español", response.Reply.Text);
        }
    }
}