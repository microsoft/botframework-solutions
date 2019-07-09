// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using EmailSkill.Adapters;
using EmailSkill.Bots;
using EmailSkill.Dialogs;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.ForwardEmail;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.ReplyEmail;
using EmailSkill.Responses.SendEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Responses.ShowEmail;
using EmailSkill.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
//using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
//using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailSkill
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

            Configuration = builder.Build();

            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        IHostingEnvironment HostingEnvironment { get; set; }

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

            // Config LG
            var path = this.HostingEnvironment.ContentRootPath;
            var resourceExplorer = ResourceExplorer.LoadProject(this.HostingEnvironment.ContentRootPath);
            services.AddSingleton(resourceExplorer);

            // Configure telemetry
            services.AddApplicationInsightsTelemetry();
            var telemetryClient = new BotTelemetryClient(new TelemetryClient());
            services.AddSingleton<IBotTelemetryClient>(telemetryClient);
            //services.AddApplicationInsightsTelemetry(telemetryClient);

            // Configure bot services
            services.AddSingleton<BotServices>();

            // Configure proactive
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();

            // Configure service manager
            services.AddTransient<IServiceManager, ServiceManager>();

            // Configure responses
            services.AddSingleton(sp => new ResponseManager(
                settings.CognitiveModels.Select(l => l.Key).ToArray(),
                new FindContactResponses(),
                new DeleteEmailResponses(),
                new ForwardEmailResponses(),
                new EmailMainResponses(),
                new ReplyEmailResponses(),
                new SendEmailResponses(),
                new EmailSharedResponses(),
                new ShowEmailResponses()));

            // register dialogs
            services.AddSingleton<MainDialog>();
            services.AddTransient<DeleteEmailDialog>();
            services.AddTransient<FindContactDialog>();
            services.AddTransient<ForwardEmailDialog>();
            services.AddTransient<ReplyEmailDialog>();
            services.AddTransient<SendEmailDialog>();
            services.AddTransient<ShowEmailDialog>();

            // Configure adapters
            services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddTransient<SkillWebSocketBotAdapter, EmailSkillWebSocketBotAdapter>();
            services.AddTransient<SkillWebSocketAdapter>();

            // Configure bot
            //services.AddSingleton<MainDialog>();
            services.AddSingleton<IBot, DialogBot<MainDialog>>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application Builder.</param>
        /// <param name="env">Hosting Environment.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //_isProduction = env.IsProduction();
            //app.UseBotApplicationInsights()
            //    .UseDefaultFiles()
            //    .UseStaticFiles()
            //    .UseWebSockets()
            //    .UseMvc();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseMvc();
        }
    }
}