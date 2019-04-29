using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Skills.Tests.Mocks;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    [TestClass]
    public class ManifestTests
    {
        private BotSettingsBase _botSettings;
        private MockHttpMessageHandler _mockHttp;
        private IServiceCollection _services;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockHttp = new MockHttpMessageHandler();

            _botSettings = new BotSettingsBase();
            _botSettings.MicrosoftAppId = Guid.NewGuid().ToString();
            _botSettings.MicrosoftAppPassword = "MockPassword";

            // Initialise the Calendar LUIS model mock configuration
            _botSettings.CognitiveModels = new Dictionary<string, BotSettingsBase.CognitiveModelConfiguration>();
            var cogModelConfig = new BotSettingsBase.CognitiveModelConfiguration();
            cogModelConfig.LanguageModels = new List<Configuration.LuisService>();

            var luisModel = new Configuration.LuisService();
            luisModel.AuthoringKey = "AUTHORINGKEY";
            luisModel.Id = "Calendar";
            luisModel.Name = "Calendar";
            luisModel.Region = "westus";
            luisModel.Version = "0.1";
            luisModel.SubscriptionKey = "SUBSCRIPTIONKEY";

            cogModelConfig.LanguageModels.Add(luisModel);
            _botSettings.CognitiveModels.Add("en", cogModelConfig);
            _botSettings.CognitiveModels.Add("de", cogModelConfig);
            _botSettings.CognitiveModels.Add("fr", cogModelConfig);

            _services = new ServiceCollection();

            _services.AddSingleton<SkillHttpAdapter, MockSkillHttpAdapter>();
			_services.AddSingleton<SkillHttpBotAdapter>();
            _services.AddSingleton<SkillWebSocketAdapter, MockSkillWebSocketAdapter>();
			_services.AddSingleton<SkillWebSocketBotAdapter>();
            _services.AddSingleton<IBotFrameworkHttpAdapter, MockBotFrameworkHttpAdapter>();
            _services.AddSingleton<IBot, MockBot>();
            _services.AddSingleton(_botSettings);
        }

        [TestMethod]
        public async Task DeserializeValidManifestFile()
        {
            using (StreamReader sr = new StreamReader("manifestTemplate.json"))
            {
                var manifestBody = await sr.ReadToEndAsync();
                JsonConvert.DeserializeObject<SkillManifest>(manifestBody);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(JsonSerializationException))]
        public async Task DeserializeInvalidManifestFile()
        {
            using (StreamReader sr = new StreamReader(@".\TestData\malformedManifestTemplate.json"))
            {
                var manifestBody = await sr.ReadToEndAsync();
                JsonConvert.DeserializeObject<SkillManifest>(manifestBody);
            }
        }

        [TestMethod]
        public async Task TestIsSkillHelper()
        {
            using (StreamReader sr = new StreamReader(@".\manifestTemplate.json"))
            {
                string manifestBody = await sr.ReadToEndAsync();
                var skillManifest = JsonConvert.DeserializeObject<SkillManifest>(manifestBody);

                List<SkillManifest> skillManifests = new List<SkillManifest>();
                skillManifests.Add(skillManifest);

                Assert.IsNotNull(SkillRouter.IsSkill(skillManifests, "calendarSkill/createEvent"));
                Assert.IsNotNull(SkillRouter.IsSkill(skillManifests, "calendarSkill/updateEvent"));
                Assert.IsNull(SkillRouter.IsSkill(skillManifests, "calendarSkill/MISSINGEVENT"));
            }
        }

        /// <summary>
        /// Test that a manifest is generated and the basic dynamic properties are changed.
        /// </summary>
        /// <returns>Task. </returns>
        [TestMethod]
        public async Task SkillControllerManifestRequest()
        {
            var controller = CreateMockSkillController();

            // Replace the NullStream with a MemoryStream
            var ms = new MemoryStream();
            controller.Response.Body = ms;

            // Invoke the Manifest request method
            await controller.SkillManifest(false);

            // MemoryStream has been closed so we read the buffer directly
            byte[] buf = ms.GetBuffer();
            string jsonResponse = Encoding.UTF8.GetString(buf, 0, buf.Length);

            try
            {
                var skillManifest = JsonConvert.DeserializeObject<SkillManifest>(jsonResponse);

                string skillUriBase = $"{controller.ControllerContext.HttpContext.Request.Scheme}://{controller.ControllerContext.HttpContext.Request.Host}";

                Assert.IsTrue(
                    skillManifest.Endpoint.ToString() == $"{skillUriBase}/api/skill/messages",
                    "Skill Manifest endpoint not set correctly");

                Assert.IsTrue(skillManifest.MSAappId == _botSettings.MicrosoftAppId, "Skill Manifest msaAppId not set correctly");

                Assert.IsTrue(skillManifest.IconUrl.ToString().StartsWith(skillUriBase), "Skill Manifest iconUrl not set correctly");
            }
            catch
            {
                // Skill manifest generation returns a reason which we capture as the reason for the test failure
                var skillManifestError = JsonConvert.DeserializeObject<string>(jsonResponse);
                Assert.Fail($"Manifest not returned from endpoint, error: '{skillManifestError}'");
            }
        }

        /// <summary>
        /// Test that when requesting inline utterances they are added to the returned manifest.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task SkillControllerManifestRequestInlineTriggerUtterances()
        {
            string luisResponse = await File.ReadAllTextAsync(@".\TestData\luisCalendarModelResponse.json");

            // Mock the call to LUIS for the model contents
            _mockHttp.When("https://westus.api.cognitive.microsoft.com*")
                    .Respond("application/json", luisResponse);

            var controller = CreateMockSkillController();

            // Replace the NullStream with a MemoryStream
            var ms = new MemoryStream();
            controller.Response.Body = ms;

            // Invoke the Manifest request method and ask for the trigger utterances to be placed inline
            await controller.SkillManifest(true);

            // MemoryStream has been closed so we read the buffer directly
            byte[] buf = ms.GetBuffer();
            string jsonResponse = Encoding.UTF8.GetString(buf, 0, buf.Length);

            try
            {
                var skillManifest = JsonConvert.DeserializeObject<SkillManifest>(jsonResponse);

                Assert.IsTrue(skillManifest.MSAappId == _botSettings.MicrosoftAppId, "Skill Manifest msaAppId not set correctly");

                // Ensure each of the registered actions has triggering utterances added
                for (int i = 0; i < 7; ++i)
                {
                    // If the trigger is an event we don't expect utterances
                    if (skillManifest.Actions[i].Definition.Triggers.Events == null)
                    {
                        Assert.IsTrue(
                            skillManifest.Actions[i].Definition.Triggers.Utterances[0].Text.Length > 0,
                            $"The {skillManifest.Actions[i].Id} action has no LUIS utterances added as part of manifest generation.");

                        // Validate DE, FR has been added too.
                        Assert.IsTrue(
                            skillManifest.Actions[i].Definition.Triggers.Utterances.Exists(u => u.Locale.ToLower() == "de"),
                            $"The {skillManifest.Actions[i].Id} action has no LUIS utterances added as part of manifest generation for the DE locale.");

                        Assert.IsTrue(
                            skillManifest.Actions[i].Definition.Triggers.Utterances.Exists(u => u.Locale.ToLower() == "fr"),
                            $"The {skillManifest.Actions[i].Id} action has no LUIS utterances added as part of manifest generation for the FR locale.");
                    }
                }
            }
            catch
            {
                // Skill manifest generation returns a reason which we capture as the reason for the test failure
                var skillManifestError = JsonConvert.DeserializeObject<string>(jsonResponse);
                Assert.Fail($"Manifest not returned from endpoint, error: '{skillManifestError}'");
            }
        }

        /// <summary>
        /// Test that when requesting inline utterances test that missing intents are detected.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task SkillControllerManifestMissingIntent()
        {
            string luisResponse = await File.ReadAllTextAsync(@".\TestData\luisCalendarModelResponse.json");

            // Mock the call to LUIS for the model contents
            _mockHttp.When("https://westus.api.cognitive.microsoft.com*")
                    .Respond("application/json", luisResponse);

            // Pass a manifest that references an intent that does not exist (MISSINGINTENT)
            var controller = CreateMockSkillController(@".\TestData\manifestInvalidIntent.json");

            // Replace the NullStream with a MemoryStream
            var ms = new MemoryStream();
            controller.Response.Body = ms;

            // Invoke the Manifest request method and ask for the trigger utterances to be placed inline
            await controller.SkillManifest(true);

            // MemoryStream has been closed so we read the buffer directly
            byte[] buf = ms.GetBuffer();
            string jsonResponse = Encoding.UTF8.GetString(buf, 0, buf.Length);

            try
            {
                var skillManifest = JsonConvert.DeserializeObject<SkillManifest>(jsonResponse);
                Assert.Fail("MISSINGINTENT was not detected as missing.");
            }
            catch
            {
                // Skill manifest generation returns a reason which we capture as the reason for the test failure
                // In this case we expect it to spot the intent which it can't find
                var skillManifestError = JsonConvert.DeserializeObject<string>(jsonResponse);

                Assert.IsTrue(skillManifestError.Contains("'MISSINGINTENT' intent which does not exist"));
            }
        }

        /// <summary>
        /// Test that when requesting inline utterances test that missing luis models are detected.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task SkillControllerManifestMissingModel()
        {
            string luisResponse = await File.ReadAllTextAsync(@".\TestData\luisCalendarModelResponse.json");

            // Mock the call to LUIS for the model contents
            _mockHttp.When("https://westus.api.cognitive.microsoft.com*")
                    .Respond("application/json", luisResponse);

            // Pass a manifest that references an intent that does not exist (MISSINGINTENT)
            var controller = CreateMockSkillController(@".\TestData\manifestInvalidLUISModel.json");

            // Replace the NullStream with a MemoryStream
            var ms = new MemoryStream();
            controller.Response.Body = ms;

            // Invoke the Manifest request method and ask for the trigger utterances to be placed inline
            await controller.SkillManifest(true);

            // MemoryStream has been closed so we read the buffer directly
            byte[] buf = ms.GetBuffer();
            string jsonResponse = Encoding.UTF8.GetString(buf, 0, buf.Length);

            try
            {
                var skillManifest = JsonConvert.DeserializeObject<SkillManifest>(jsonResponse);
                Assert.Fail("MISSINGLUISMODEL was not detected as missing.");
            }
            catch
            {
                // Skill manifest generation returns a reason which we capture as the reason for the test failure
                // In this case we expect it to spot the intent which it can't find
                var skillManifestError = JsonConvert.DeserializeObject<string>(jsonResponse);

                Assert.IsTrue(skillManifestError.Contains("'MISSINGLUISMODEL' model which cannot be found in the currently deployed configuration"));
            }
        }

        private MockSkillController CreateMockSkillController(string manifestFileOverride = null)
		{
			var sp = _services.BuildServiceProvider();
			var botFrameworkHttpAdapter = sp.GetService<IBotFrameworkHttpAdapter>();
			var skillHttpAdapter = sp.GetService<SkillHttpAdapter>();
			var skillWebSocketAdapter = sp.GetService<SkillWebSocketAdapter>();
			var bot = sp.GetService<IBot>();
			var controller = new MockSkillController(botFrameworkHttpAdapter, skillHttpAdapter, skillWebSocketAdapter, bot, _botSettings, _mockHttp.ToHttpClient(), manifestFileOverride);

			controller.ControllerContext = new ControllerContext();
			controller.ControllerContext.HttpContext = new DefaultHttpContext();
			controller.ControllerContext.HttpContext.Request.Scheme = "https";
			controller.ControllerContext.HttpContext.Request.Host = new HostString("virtualassistant.azurewebsites.net");

			return controller;
		}
	}
}