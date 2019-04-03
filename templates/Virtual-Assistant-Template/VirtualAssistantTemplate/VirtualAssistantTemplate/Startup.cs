// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using VirtualAssistantTemplate.Dialogs.Main.Resources;
using VirtualAssistantTemplate.Middleware;
using VirtualAssistantTemplate.Middleware.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VirtualAssistantTemplate.Configuration;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Skills.Auth;

namespace VirtualAssistantTemplate
{
    public class Startup
    {
        private ILoggerFactory _loggerFactory;
        private readonly bool _isProduction = false;

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _isProduction = env.IsProduction();
            _loggerFactory = loggerFactory;

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
            var settings = new BotSettings();
            Configuration.Bind(settings);

            var botServices = new BotServices(settings);
            services.AddSingleton(botServices);

            // Use Application Insights
            var botTelemetryClient = new BotTelemetryClient(new TelemetryClient(new TelemetryConfiguration(settings.AppInsights.InstrumentationKey)));
            services.AddBotApplicationInsights(botTelemetryClient);

            // Initialize Bot State
            var cosmosOptions = new CosmosDbStorageOptions()
            {
                CosmosDBEndpoint = new Uri(settings.CosmosDb.Endpoint),
                AuthKey = settings.CosmosDb.Key,
                CollectionId = settings.CosmosDb.Collection,
                DatabaseId = settings.CosmosDb.Database,
            };
            var dataStore = new CosmosDbStorage(cosmosOptions);
            var userState = new UserState(dataStore);
            var conversationState = new ConversationState(dataStore);

            services.AddSingleton(dataStore);
            services.AddSingleton(userState);
            services.AddSingleton(conversationState);
            services.AddSingleton(new BotStateSet(userState, conversationState));

            var microsoftAppCredentials = new MicrosoftAppCredentials(settings.MicrosoftAppId, settings.MicrosoftAppPassword);
            services.AddSingleton(microsoftAppCredentials);

            // Add the bot with options
            services.AddBot<Bot>(options =>
            {
                options.CredentialProvider = new SimpleCredentialProvider(settings.MicrosoftAppId, settings.MicrosoftAppPassword);

                var appInsightsLogger = new TelemetryLoggerMiddleware(botTelemetryClient, logPersonalInformation: true);
                options.Middleware.Add(appInsightsLogger);

                // Catches any errors that occur during a conversation turn and logs them to AppInsights.
                options.OnTurnError = async (context, exception) =>
                {
                    botTelemetryClient.TrackException(exception);
                    await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.Message}" ));
                    await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.StackTrace}"));
                    await context.SendActivityAsync(MainStrings.ERROR);
                };

                // Transcript Middleware (saves conversation history in a standard format)
                var transcriptStore = new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container);
                var transcriptMiddleware = new TranscriptLoggerMiddleware(transcriptStore);
                options.Middleware.Add(transcriptMiddleware);

                // Typing Middleware (automatically shows typing when the bot is responding/working)
                options.Middleware.Add(new ShowTypingMiddleware());

                // Locale Middleware (sets UI culture based on Activity.Locale)
                options.Middleware.Add(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));

                // Autosave State Middleware (saves bot state after each turn)
                options.Middleware.Add(new AutoSaveStateMiddleware(userState, conversationState));
            });
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
                .UseBotFramework();
        }
    }
}
