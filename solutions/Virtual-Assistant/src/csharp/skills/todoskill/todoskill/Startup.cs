// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Shared;
using Microsoft.Bot.Builder.Solutions.Shared.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ToDoSkill.Adapters;
using ToDoSkill.Bots;
using ToDoSkill.Dialogs;
using ToDoSkill.Responses.AddToDo;
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Responses.Main;
using ToDoSkill.Responses.MarkToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Responses.ShowToDo;
using ToDoSkill.Services;

namespace ToDoSkill
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("cognitivemodels.json", optional: true)
                .AddJsonFile($"cognitivemodels.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("skills.json", optional: true)
                .AddJsonFile($"skills.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

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
            services.AddSingleton<ProactiveState>();
            services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            // Configure telemetry
            var telemetryClient = new BotTelemetryClient(new TelemetryClient(settings.AppInsights));
            services.AddSingleton<IBotTelemetryClient>(telemetryClient);
            services.AddBotApplicationInsights(telemetryClient);

            // Configure proactive
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();

            // Configure service manager
            services.AddTransient<IServiceManager, ServiceManager>();

            // Configure responses
            services.AddSingleton(sp => new ResponseManager(
                settings.CognitiveModels.Select(l => l.Key).ToArray(),
                new AddToDoResponses(),
                new DeleteToDoResponses(),
                new ToDoMainResponses(),
                new MarkToDoResponses(),
                new ToDoSharedResponses(),
                new ShowToDoResponses()));

            // Configure Skill authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = "https://login.microsoftonline.com/microsoft.com";
                options.Audience = settings.MicrosoftAppId;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0",
                };
            });

            // comment out for now to disable whitelist checking
            // services.AddSingleton<ISkillAuthProvider, JwtClaimAuthProvider>();
            // services.AddSingleton<ISkillWhitelist, SkillWhitelist>();

            // Configure adapters
            services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddTransient<SkillAdapter, ToDoSkillAdapter>();

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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseBotApplicationInsights()
                .UseDefaultFiles()
                .UseStaticFiles()
                .UseAuthentication()
                .UseMvc();
        }
    }
}