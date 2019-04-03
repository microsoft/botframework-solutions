// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DeleteEmail.Resources;
using EmailSkill.Dialogs.FindContact.Resources;
using EmailSkill.Dialogs.ForwardEmail.Resources;
using EmailSkill.Dialogs.Main.Resources;
using EmailSkill.Dialogs.ReplyEmail.Resources;
using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkill.ServiceClients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Auth;
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
using Microsoft.IdentityModel.Tokens;

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
                .AddEnvironmentVariables();

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
            var botConfig = BotConfiguration.Load(botFilePath ?? @".\EmailSkill.bot", botFileSecret);
            services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded."));

            // Use Application Insights
            services.AddBotApplicationInsights(botConfig);

            // Initializes your bot service clients and adds a singleton that your Bot can access through dependency injection.
            var parameters = Configuration.GetSection("parameters")?.Get<string[]>();
            var configuration = Configuration.GetSection("configuration")?.GetChildren()?.ToDictionary(x => x.Key, y => y.Value as object);
            var supportedProviders = Configuration.GetSection("supportedProviders")?.Get<string[]>();
            var languageModels = Configuration.GetSection("languageModels").Get<Dictionary<string, Dictionary<string, string>>>();
            var connectedServices = new SkillConfiguration(botConfig, languageModels, supportedProviders, parameters, configuration);
            services.AddSingleton<SkillConfigurationBase>(sp => connectedServices);

            var supportedLanguages = languageModels.Select(l => l.Key).ToArray();
            var responseManager = new ResponseManager(
                supportedLanguages,
                new FindContactResponses(),
                new DeleteEmailResponses(),
                new ForwardEmailResponses(),
                new EmailMainResponses(),
                new ReplyEmailResponses(),
                new SendEmailResponses(),
                new EmailSharedResponses(),
                new ShowEmailResponses());

            // Register bot responses for all supported languages.
            services.AddSingleton(sp => responseManager);

            var defaultLocale = Configuration.GetSection("defaultLocale").Get<string>();

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

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = "https://login.microsoftonline.com/microsoft.com";
                options.Audience = endpointService.AppId;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0",
                };
            });

            services.AddSingleton<ISkillAuthProvider, JwtClaimAuthProvider>();
            services.AddSingleton<ISkillWhitelist, SkillWhitelist>();

            // Initialize Email client
            services.AddSingleton<IServiceManager, ServiceManager>();

            // Add the bot
            services.AddTransient<IBot, EmailSkill>();

            var credentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

            // Create the middlewares
            var telemetryClient = services.BuildServiceProvider().GetService<IBotTelemetryClient>();
            var appInsightsLogger = new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true);

            var storageService = botConfig.Services.FirstOrDefault(s => s.Type == ServiceTypes.BlobStorage) ?? throw new Exception("Please configure your Azure Storage service in your .bot file.");
            var blobStorage = storageService as BlobStorageService;
            var transcriptStore = new AzureBlobTranscriptStore(blobStorage.ConnectionString, blobStorage.Container);
            var transcriptMiddleware = new TranscriptLoggerMiddleware(transcriptStore);

            var typingMiddleware = new ShowTypingMiddleware();
            var setLocaleMiddleware = new SetLocaleMiddleware(defaultLocale ?? "en-us");
            var eventDebuggerMiddleware = new EventDebuggerMiddleware();
            var autoSaveStateMiddleware = new AutoSaveStateMiddleware(userState, conversationState);

            Func<ITurnContext, Exception, Task> onTurnError = async (context, exception) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(context.Activity.Locale);
                await context.SendActivityAsync(responseManager.GetResponse(EmailSharedResponses.EmailErrorMessage));
                await context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Email Skill Error: {exception.Message} | {exception.StackTrace}"));
                telemetryClient.TrackExceptionEx(exception, context.Activity);
            };

            // Add the http adapter with middlewares
            services.AddTransient<IBotFrameworkHttpAdapter>(sp =>
            {
                var botFrameworkHttpAdapter = new BotFrameworkHttpAdapter(credentialProvider)
                {
                    OnTurnError = onTurnError
                };

                botFrameworkHttpAdapter.Use(appInsightsLogger);
                botFrameworkHttpAdapter.Use(transcriptMiddleware);
                botFrameworkHttpAdapter.Use(typingMiddleware);
                botFrameworkHttpAdapter.Use(setLocaleMiddleware);
                botFrameworkHttpAdapter.Use(eventDebuggerMiddleware);
                botFrameworkHttpAdapter.Use(autoSaveStateMiddleware);

                return botFrameworkHttpAdapter;
            });

            // Add the SkillAdapter with middlewares
            services.AddTransient(sp =>
            {
                var skillAdapter = new SkillAdapter(credentialProvider)
                {
                    OnTurnError = onTurnError
                };

                skillAdapter.Use(appInsightsLogger);
                skillAdapter.Use(transcriptMiddleware);
                skillAdapter.Use(typingMiddleware);
                skillAdapter.Use(setLocaleMiddleware);
                skillAdapter.Use(eventDebuggerMiddleware);
                skillAdapter.Use(autoSaveStateMiddleware);

                return skillAdapter;
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
                .UseAuthentication()
                .UseMvc();
        }
    }
}