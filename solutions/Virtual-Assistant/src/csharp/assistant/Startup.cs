// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistant.Dialogs.Main;
using VirtualAssistant.Dialogs.Main.Resources;

namespace VirtualAssistant
{
    public class Startup
    {
        private const string SkillEventsConfigFile = "skillEvents.json";
        private const string SkillEventsConfigName = "skillEvents";
        private bool _isProduction = false;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (File.Exists(Path.Combine(env.ContentRootPath, SkillEventsConfigFile)))
            {
                builder.AddJsonFile(SkillEventsConfigFile, optional: true);
            }

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

            // add background task queue
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();

            // Load the connected services from .bot file.
            var botFilePath = Configuration.GetSection("botFilePath")?.Value;
            var botFileSecret = Configuration.GetSection("botFileSecret")?.Value;
            var botConfig = BotConfiguration.Load(botFilePath ?? throw new Exception("Please configure your bot file path in appsettings.json."), botFileSecret);

            // Use Application Insights
            services.AddBotApplicationInsights(botConfig);

            // Initializes your bot service clients and adds a singleton that your Bot can access through dependency injection.
            var languageModels = Configuration.GetSection("languageModels").Get<Dictionary<string, Dictionary<string, string>>>();
            var skills = Configuration.GetSection("skills").Get<List<SkillDefinition>>();
            List<SkillEvent> skillEvents = null;
            var skillEventsConfig = Configuration.GetSection(SkillEventsConfigName);
            if (skillEventsConfig != null)
            {
                skillEvents = skillEventsConfig.Get<List<SkillEvent>>();
            }

            var connectedServices = new BotServices(botConfig, languageModels, skills, skillEvents);
            services.AddSingleton(sp => connectedServices);

            var defaultLocale = Configuration.GetSection("configuration").Get<string>();
            var supportedLanguages = languageModels.Select(l => l.Key).ToArray();
            var responseManager = new ResponseManager(
                supportedLanguages,
                new MainDialogResponses());

            // Register bot responses for all supported languages.
            services.AddSingleton(sp => responseManager);

            // Initialize Bot State
            var cosmosDbService = botConfig.Services.FirstOrDefault(s => s.Type == ServiceTypes.CosmosDB) ?? throw new Exception("Please configure your CosmosDb service in your .bot file.");
            var cosmosDb = cosmosDbService as CosmosDbService;
            var cosmosOptions = new CosmosDbStorageOptions()
            {
                CosmosDBEndpoint = new Uri(cosmosDb.Endpoint),
                AuthKey = cosmosDb.Key,
                CollectionId = cosmosDb.Collection,
                DatabaseId = cosmosDb.Database,
            };
            var dataStore = new CosmosDbStorage(cosmosOptions);
            var userState = new UserState(dataStore);
            var conversationState = new ConversationState(dataStore);
            var proactiveState = new ProactiveState(dataStore);

            services.AddSingleton(dataStore);
            services.AddSingleton(userState);
            services.AddSingleton(conversationState);
            services.AddSingleton(proactiveState);
            services.AddSingleton(new BotStateSet(userState, conversationState));

            var environment = _isProduction ? "production" : "development";
            var service = botConfig.Services.FirstOrDefault(s => s.Type == ServiceTypes.Endpoint && s.Name == environment);
            if (!(service is EndpointService endpointService))
            {
                throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
            }

            services.AddSingleton(endpointService);

            services.AddSingleton<IBot, VirtualAssistant>();

            // Add the http adapter to enable MVC style bot API
            services.AddSingleton<IBotFrameworkHttpAdapter>((sp) =>
            {
                var credentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                var telemetryClient = sp.GetService<IBotTelemetryClient>();
                var botFrameworkHttpAdapter = new BotFrameworkHttpAdapter(credentialProvider)
                {
                    OnTurnError = async (context, exception) =>
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                        var responseBuilder = new MainResponses();
                        await responseBuilder.ReplyWith(context, MainResponses.ResponseIds.Error);
                        await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Virtual Assistant Error: {exception.Message} | {exception.StackTrace}"));
                        telemetryClient.TrackExceptionEx(exception, context.Activity);
                    }
                };

                // Telemetry Middleware (logs activity messages in Application Insights)
                var appInsightsLogger = new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true);
                botFrameworkHttpAdapter.Use(appInsightsLogger);

                // Transcript Middleware (saves conversation history in a standard format)
                var storageService = botConfig.Services.FirstOrDefault(s => s.Type == ServiceTypes.BlobStorage) ?? throw new Exception("Please configure your Azure Storage service in your .bot file.");
                var blobStorage = storageService as BlobStorageService;
                var transcriptStore = new AzureBlobTranscriptStore(blobStorage.ConnectionString, blobStorage.Container);
                var transcriptMiddleware = new TranscriptLoggerMiddleware(transcriptStore);
                botFrameworkHttpAdapter.Use(transcriptMiddleware);

                // Typing Middleware (automatically shows typing when the bot is responding/working)
                botFrameworkHttpAdapter.Use(new ShowTypingMiddleware());
                botFrameworkHttpAdapter.Use(new SetLocaleMiddleware(defaultLocale ?? "en-us"));
                botFrameworkHttpAdapter.Use(new EventDebuggerMiddleware());
                botFrameworkHttpAdapter.Use(new AutoSaveStateMiddleware(userState, conversationState));
                botFrameworkHttpAdapter.Use(new ProactiveStateMiddleware(proactiveState));

                return botFrameworkHttpAdapter;
            });
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
                .UseMvc();
        }
    }
}