// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Solutions.Tests.Responses
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class LanguageGenerationTests
    {
        private Dictionary<string, List<string>> localeLgFiles;
        private LocaleLGFileManager localeTemplateEngineManager;

        [TestInitialize]
        public void Setup()
        {
            localeLgFiles = new Dictionary<string, List<string>>
            {
                { "en-us", new List<string>() { Path.Combine(".", "Responses", "TestResponses.lg") } },
                { "es-es", new List<string>() { Path.Combine(".", "Responses", "TestResponses.es.lg") } },
            };

            localeTemplateEngineManager = new LocaleLGFileManager(localeLgFiles, "en-us");
        }

        [TestMethod]
        public void GetResponseWithLanguageGeneration_English()
        {
            var defaultCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = new CultureInfo("en-us");

            // Generate English response using LG with data
            dynamic data = new JObject();
            data.Name = "Darren";
            var response = localeTemplateEngineManager.GenerateActivityForLocale("HaveNameMessage", data);

            // Retrieve possible responses directly from the correct template to validate logic
            var possibleResponses = localeTemplateEngineManager.LGFilesPerLocale["en-us"].ExpandTemplate("HaveNameMessage", data);

            Assert.IsTrue(possibleResponses.Contains(response.Text));

            CultureInfo.CurrentUICulture = defaultCulture;
        }

        [TestMethod]
        public void GetResponseWithLanguageGeneration_Spanish()
        {
            var defaultCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = new CultureInfo("es-es");

            // Generate English response using LG with data
            dynamic data = new JObject();
            data.Name = "Darren";
            var response = localeTemplateEngineManager.GenerateActivityForLocale("HaveNameMessage", data);

            // Retrieve possible responses directly from the correct template to validate logic
            var possibleResponses = localeTemplateEngineManager.LGFilesPerLocale["es-es"].ExpandTemplate("HaveNameMessage", data);

            Assert.IsTrue(possibleResponses.Contains(response.Text));

            CultureInfo.CurrentUICulture = defaultCulture;
        }

        [TestMethod]
        public void GetResponseWithLanguageGeneration_Fallback()
        {
            var defaultCulture = CultureInfo.CurrentUICulture;

            // German locale not supported, locale template engine should fallback to english as per default in Test Setup.
            CultureInfo.CurrentUICulture = new CultureInfo("de-de");

            // Generate English response using LG with data
            dynamic data = new JObject();
            data.Name = "Darren";
            var response = localeTemplateEngineManager.GenerateActivityForLocale("HaveNameMessage", data);

            // Retrieve possible responses directly from the correct template to validate logic
            // Logic should fallback to english due to unsupported locale
            var possibleResponses = localeTemplateEngineManager.LGFilesPerLocale["en-us"].ExpandTemplate("HaveNameMessage", data);

            Assert.IsTrue(possibleResponses.Contains(response.Text));

            CultureInfo.CurrentUICulture = defaultCulture;
        }
    }
}
