// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Solutions.Testing
{
    public abstract class BotTestBase
    {
        private static readonly Regex ResponseTokensRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        public IContainer Container { get; set; }

        public IConfigurationRoot Configuration { get; set; }

        public BotResponseBuilder BotResponseBuilder { get; set; }

        public abstract IBot BuildBot();

        public virtual void Initialize()
        {
            this.Configuration = new BuildConfig().Configuration;

            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConfiguration>(this.Configuration);

            this.Container = builder.Build();

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
        }

        protected TestFlow TestFlow(IMiddleware intentRecognizerMiddleware)
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);
            var userState = new UserState(storage);
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(userState, convState))
                .Use(new ConsoleOutputMiddleware());

            if (intentRecognizerMiddleware != null)
            {
                adapter.Use(intentRecognizerMiddleware);
            }

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot();
                await bot.OnTurnAsync(context, token);
            });

            return testFlow;
        }

        protected TestFlow TestEventFlow()
        {
            return this.TestFlow((IMiddleware)null);
        }

        protected string[] ParseReplies(Reply[] replies, string[] tokens)
        {
            var responses = new string[replies.Length];
            for (var i = 0; i < replies.Length; i++)
            {
                var tokenIndex = i;
                var replacedString = ResponseTokensRegex.Replace(replies[i].Text, match => tokens[tokenIndex]);
                responses[i] = replacedString;
            }

            return responses;
        }

        protected string[] ParseReplies(Reply[] replies, StringDictionary tokens = null)
        {
            var responses = new string[replies.Length];
            if (tokens == null)
            {
                return responses;
            }

            for (var i = 0; i < replies.Length; i++)
            {
                responses[i] = this.BotResponseBuilder.Format(replies[i].Text, tokens);
            }

            return responses;
        }

        protected string[] ParseRepliesSpeak(Reply[] replies, StringDictionary tokens = null)
        {
            var responses = new string[replies.Length];
            if (tokens == null)
            {
                return responses;
            }

            for (var i = 0; i < replies.Length; i++)
            {
                responses[i] = this.BotResponseBuilder.Format(replies[i].Speak, tokens);
            }

            return responses;
        }
    }
}