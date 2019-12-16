﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalendarSkill.Adapters;
using CalendarSkill.Bots;
using CalendarSkill.Dialogs;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.CheckAvailable;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.JoinEvent;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Responses.TimeRemaining;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CalendarSkill
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

            // Configure server options
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

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
            services.AddSingleton<ProactiveState>();
            services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState, proactiveState);
            });

            // Configure localized responses
            var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };
            var templateFiles = new Dictionary<string, string>
            {
                { "ChangeEventStatus", "ChangeEventStatusDialogActivities" },
                { "CheckAvailable", "CheckAvailableActivities" },
                { "CreateEvent", "CreateEventDialogActivities" },
                { "FindContact", "FindContactDialogActivities" },
                { "JoinEvent", "JoinEventDialogActivities" },
                { "Main", "MainDialogActivities" },
                { "Shared", "SharedActivities" },
                { "Summary", "SummaryDialogActivities" },
                { "TimeRemaining", "TimeRemainingDialogActivities" },
                { "UpcomingEvent", "UpcomingEventDialogActivities" },
                { "UpdateEvent", "UpdateEventDialogActivities" },
            };

            var localizedTemplates = new Dictionary<string, List<string>>();
            foreach (var locale in supportedLocales)
            {
                var localeTemplateFiles = new List<string>();
                foreach (var (dialog, template) in templateFiles)
                {
                    // LG template for default locale should not include locale in file extension.
                    if (locale.Equals(settings.DefaultLocale ?? "en-us"))
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

            services.AddSingleton(new LocaleTemplateEngineManager(localizedTemplates, settings.DefaultLocale ?? "en-us"));

            // Configure telemetry
            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();
            services.AddSingleton<TelemetryInitializerMiddleware>();
            services.AddSingleton<TelemetryLoggerMiddleware>();

            // Configure bot services
            services.AddSingleton<BotServices>();

            // Configure proactive
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();

            // Configure service manager
            services.AddTransient<IServiceManager, ServiceManager>();

            // register dialogs
            services.AddTransient<MainDialog>();
            services.AddTransient<ChangeEventStatusDialog>();
            services.AddTransient<JoinEventDialog>();
            services.AddTransient<CreateEventDialog>();
            services.AddTransient<FindContactDialog>();
            services.AddTransient<ShowEventsDialog>();
            services.AddTransient<TimeRemainingDialog>();
            services.AddTransient<UpcomingEventDialog>();
            services.AddTransient<UpdateEventDialog>();
            services.AddTransient<CheckAvailableDialog>();

            // Configure adapters
            services.AddTransient<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddTransient<SkillWebSocketBotAdapter, CalendarSkillWebSocketBotAdapter>();
            services.AddTransient<SkillWebSocketAdapter>();

            services.AddSingleton<IWhitelistAuthenticationProvider, WhitelistAuthenticationProvider>();

            // Configure bot
            services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();
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