// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
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
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PointOfInterestSkill.Adapters;
using PointOfInterestSkill.Bots;
using PointOfInterestSkill.Dialogs;
using PointOfInterestSkill.Responses.CancelRoute;
using PointOfInterestSkill.Responses.FindPointOfInterest;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;

namespace PointOfInterestSkill
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
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
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure MVC
            services.AddControllers();

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

            // Configure service manager
            services.AddTransient<IServiceManager, ServiceManager>();

            // Configure HttpContext required for path resolution
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Configure responses
            services.AddSingleton(sp => new ResponseManager(
                settings.CognitiveModels.Select(l => l.Key).ToArray(),
                new CancelRouteResponses(),
                new FindPointOfInterestResponses(),
                new POIMainResponses(),
                new RouteResponses(),
                new POISharedResponses()));

            // register dialogs
            services.AddTransient<MainDialog>();
            services.AddTransient<CancelRouteDialog>();
            services.AddTransient<FindParkingDialog>();
            services.AddTransient<FindPointOfInterestDialog>();
            services.AddTransient<RouteDialog>();
            services.AddTransient<GetDirectionsDialog>();

            // Configure adapters
            services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddTransient<SkillWebSocketBotAdapter, POISkillWebSocketBotAdapter>();
            services.AddTransient<SkillWebSocketAdapter>();

            // Register WhiteListAuthProvider
            services.AddSingleton<IWhitelistAuthenticationProvider, WhitelistAuthenticationProvider>();

            // Configure bot
            services.AddTransient<MainDialog>();
            services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application Builder.</param>
        /// <param name="env">Hosting Environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}