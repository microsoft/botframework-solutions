﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsSkill.Adapters;
using NewsSkill.Bots;
using NewsSkill.Dialogs;
using NewsSkill.Services;

namespace NewsSkill
{
    public class Startup
    {
        private bool _isProduction = false;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("cognitivemodels.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"cognitivemodels.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("skills.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"skills.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_2);
            var provider = services.BuildServiceProvider();

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);
            services.AddSingleton<BotSettings>(settings);
            services.AddSingleton<BotSettingsBase>(settings);

            // Configure credentials
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddSingleton(new MicrosoftAppCredentials(settings.MicrosoftAppId, settings.MicrosoftAppPassword));

            // Configure bot state
            services.AddSingleton<IStorage>(new CosmosDbStorage(settings.CosmosDb));
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();
            services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                return new BotStateSet(userState, conversationState);
            });

            // Configure telemetry
            services.AddApplicationInsightsTelemetry();
            var telemetryClient = new BotTelemetryClient(new TelemetryClient());
            services.AddSingleton<IBotTelemetryClient>(telemetryClient);
            services.AddBotApplicationInsights(telemetryClient);

            // Configure bot services
            services.AddSingleton<BotServices>();

            // Configure proactive
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();

            // Configure Azure maps services
            services.AddSingleton<AzureMapsService>();

            // Configure HttpContext required for path resolution
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // register dialogs
            services.AddTransient<MainDialog>();
            services.AddTransient<FindArticlesDialog>();
            services.AddTransient<TrendingArticlesDialog>();
            services.AddTransient<FavoriteTopicsDialog>();

            // Configure adapters
            services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddTransient<SkillWebSocketBotAdapter, NewsSkillWebSocketBotAdapter>();
            services.AddTransient<SkillWebSocketAdapter>();

            // Configure bot
            services.AddTransient<MainDialog>();
            services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application Builder.</param>
        /// <param name="env">Hosting Environment.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();
            app.UseBotApplicationInsights()
                .UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseMvc();
        }
    }
}