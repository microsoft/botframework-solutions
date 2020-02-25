// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using EmailSkill.Bots;
using EmailSkill.Dialogs;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Tests.Flow.Fakes;
using EmailSkill.Tests.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    public class EmailSkillTestBase : BotTestBase
    {
        public static readonly string Provider = "Azure Active Directory v2";

        public IServiceCollection Services { get; set; }

        public MockServiceManager ServiceManager { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize mock service manager
            ServiceManager = new MockServiceManager();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = Provider, Provider = Provider }
                }
            });

            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en-us", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, LuisRecognizer>
                            {
                                { "General", new MockGeneralLuisRecognizer() },
                                {
                                    "Email", new MockEmailLuisRecognizer(
                                        new ForwardEmailUtterances(),
                                        new ReplyEmailUtterances(),
                                        new DeleteEmailUtterances(),
                                        new SendEmailUtterances(),
                                        new ShowEmailUtterances())
                                }
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(new MicrosoftAppCredentials(string.Empty, string.Empty));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            Services.AddSingleton<TestAdapter>(sp =>
            {
                var adapter = new DefaultTestAdapter();
                adapter.AddUserToken("Azure Active Directory v2", Channels.Test, "user1", "test");
                return adapter;
            });

            // Configure localized responses
            var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };
            var templateFiles = new Dictionary<string, string>
            {
                { "Shared", "ResponsesAndTexts" },
            };

            var localizedTemplates = new Dictionary<string, List<string>>();
            foreach (var locale in supportedLocales)
            {
                var localeTemplateFiles = new List<string>();
                foreach (var (dialog, template) in templateFiles)
                {
                    // LG template for default locale should not include locale in file extension.
                    if (locale.Equals("en-us"))
                    {
                        localeTemplateFiles.Add(Path.Combine(".", "Responses", dialog, $"{template}.lg"));
                    }
                    else
                    {
                        localeTemplateFiles.Add(Path.Combine(".", "Responses", dialog, $"{template}.{locale}.lg"));
                    }
                }

                localizedTemplates.Add(locale, localeTemplateFiles);
            }

            Services.AddSingleton(new LocaleTemplateEngineManager(localizedTemplates, "en-us"));

            // Configure files for generating all responses. Response from bot should equal one of them.
            var engineAll = new TemplateEngine().AddFile(Path.Combine("Responses", "Shared", "ResponsesAndTexts.lg"));
            Services.AddSingleton(engineAll);

            Services.AddSingleton<IStorage>(new MemoryStorage());

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<IServiceManager>(ServiceManager);

            Services.AddTransient<MainDialog>();
            Services.AddTransient<DeleteEmailDialog>();
            Services.AddTransient<FindContactDialog>();
            Services.AddTransient<ForwardEmailDialog>();
            Services.AddTransient<ReplyEmailDialog>();
            Services.AddTransient<SendEmailDialog>();
            Services.AddTransient<ShowEmailDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            ConfigData.GetInstance().MaxDisplaySize = 3;
            ConfigData.GetInstance().MaxReadSize = 3;
        }

        public string[] GetTemplates(string templateName, object data = null)
        {
            var sp = Services.BuildServiceProvider();
            var engine = sp.GetService<TemplateEngine>();
            var formatTemplateName = templateName + ".Text";
            return engine.ExpandTemplate(formatTemplateName, data).ToArray();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();

            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(Provider, Channels.Test, adapter.Conversation.User.Id, "test");

            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await stateAccessor.GetAsync(context, () => new EmailSkillState());
                state.MailSourceType = MailSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public TestFlow GetSkillTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                // Set claims in turn state to simulate skill mode
                var claims = new List<Claim>();
                claims.Add(new Claim(AuthenticationConstants.VersionClaim, "1.0"));
                claims.Add(new Claim(AuthenticationConstants.AudienceClaim, Guid.NewGuid().ToString()));
                claims.Add(new Claim(AuthenticationConstants.AppIdClaim, Guid.NewGuid().ToString()));
                context.TurnState.Add("BotIdentity", new ClaimsIdentity(claims));

                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected Action<IActivity> CheckForEoC(bool value = false, bool actionSuccess = true)
        {
            return activity =>
            {
                var eoc = (Activity)activity;
                Assert.AreEqual(ActivityTypes.EndOfConversation, eoc.Type);
                if (value)
                {
                    if (eoc.Value is ActionResult)
                    {
                        var actionResult = eoc.Value as ActionResult;
                        Assert.IsNotNull(actionResult);
                        Assert.AreEqual(actionSuccess, actionResult.ActionSuccess);
                    }
                    else if (eoc.Value is SummaryResult)
                    {
                        var actionResult = eoc.Value as SummaryResult;
                        Assert.IsNotNull(actionResult);
                        Assert.AreEqual(actionSuccess, actionResult.ActionSuccess);
                        Assert.AreEqual(actionResult.EmailList.Count, 5);
                    }
                }
            };
        }

        protected string[] CancelResponses()
        {
            return GetTemplates(EmailSharedResponses.CancellingMessage);
        }
    }
}