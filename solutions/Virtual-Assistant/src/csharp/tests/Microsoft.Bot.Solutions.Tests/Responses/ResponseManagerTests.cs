using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Tests.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests
{
    [TestClass]
    public class ResponseManagerTests
    {
        private string _resourceDir;
        private CultureInfo _currentCulture;
        private ResponseManager _responseManager;

        [TestInitialize]
        public void Initialize()
        {
            _currentCulture = CultureInfo.CurrentUICulture;
            _resourceDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Dialogs");
            _responseManager = new ResponseManager(
                new IResponseIdCollection[] { new TestResponses() },
                new string[] { "en", "es" });
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
            var copy1 = _responseManager.GetResponseTemplate("GetResponseText");
            var copy2 = _responseManager.GetResponseTemplate("GetResponseText");
            Assert.AreEqual(copy1.Replies[0].Text, copy2.Replies[0].Text);

            copy2.Replies[0].Text = "Something different";
            Assert.AreNotEqual(copy1.Replies[0].Text, copy2.Replies[0].Text);
        }

        [TestMethod]
        public void KeyNotFoundThrowsException()
        {
            Assert.ThrowsException<KeyNotFoundException>(() =>
            {
                _responseManager.GetResponseTemplate("NotThere");
            });
        }

        [TestMethod]
        public void InputHintDefaultsToAcceptingInput()
        {
            var response = _responseManager.GetResponseTemplate("NoInputHint");
            Assert.AreEqual(InputHints.AcceptingInput, response.InputHint);
        }

        [TestMethod]
        public void LanguageFallback()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("es-MX");
            var response = _responseManager.GetResponseTemplate("GetResponseText");
            Assert.AreEqual("El texto", response.Reply.Text);

            response = _responseManager.GetResponseTemplate("EnglishOnly");
            Assert.AreEqual("This wasn't found in spanish so the fallback answer is returned", response.Reply.Text);
        }

        [TestMethod]
        public void MultiLanguage()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var response = _responseManager.GetResponseTemplate("MultiLanguage");
            Assert.AreEqual("This is in English", response.Reply.Text);

            CultureInfo.CurrentUICulture = new CultureInfo("es-MX");
            response = _responseManager.GetResponseTemplate("MultiLanguage");
            Assert.AreEqual("Esto sería en español", response.Reply.Text);
        }
    }
}