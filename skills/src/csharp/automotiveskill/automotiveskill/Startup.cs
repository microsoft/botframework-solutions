// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using System.Linq;
    using AutomotiveSkill.Adapters;
    using AutomotiveSkill.Bots;
    using AutomotiveSkill.Dialogs;
    using AutomotiveSkill.Responses.Main;
    using AutomotiveSkill.Responses.Shared;
    using AutomotiveSkill.Responses.VehicleSettings;
    using AutomotiveSkill.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.ApplicationInsights;
    using Microsoft.Bot.Builder.Azure;
    using Microsoft.Bot.Builder.BotFramework;
    using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Builder.Skills;
    using Microsoft.Bot.Builder.Solutions;
    using Microsoft.Bot.Builder.Solutions.Responses;
    using Microsoft.Bot.Builder.Solutions.TaskExtensions;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

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
            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);
            services.AddSingleton<BotSettings>(settings);
            services.AddSingleton<BotSettingsBase>(settings);

            // Configure bot services
            services.AddSingleton<BotServices>();

            // Configure credentials
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

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
            var telemetryClient = new BotTelemetryClient(new TelemetryClient(settings.AppInsights));
            services.AddSingleton<IBotTelemetryClient>(telemetryClient);
            services.AddBotApplicationInsights(telemetryClient);

            // Configure proactive
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();

            // Configure responses
            services.AddSingleton(sp => new ResponseManager(
                settings.CognitiveModels.Select(l => l.Key).ToArray(),
                new AutomotiveSkillMainResponses(),
                new AutomotiveSkillSharedResponses(),
                new VehicleSettingsResponses()));

            // register dialogs
            services.AddTransient<MainDialog>();
            services.AddTransient<VehicleSettingsDialog>();

            // Configure adapters
            services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddTransient<SkillWebSocketBotAdapter, AutomotiveSkillWebSocketBotAdapter>();
            services.AddTransient<SkillWebSocketAdapter>();
            services.AddTransient<SkillHttpBotAdapter, AutomotiveSkillHttpBotAdapter>();
            services.AddTransient<SkillHttpAdapter>();

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