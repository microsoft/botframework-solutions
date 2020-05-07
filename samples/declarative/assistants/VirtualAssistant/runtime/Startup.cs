// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using BotProject.Middleware;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Bot.Builder.ComposerBot.Json
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.HostingEnvironment = env;
            this.Configuration = configuration;
        }

        public IWebHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        public void ConfigureTranscriptLoggerMiddleware(BotFrameworkHttpAdapter adapter, BotSettings settings)
        {
            if (settings.Feature.UseTranscriptLoggerMiddleware)
            {
                if (!string.IsNullOrEmpty(settings.BlobStorage.ConnectionString) && !string.IsNullOrEmpty(settings.BlobStorage.Container))
                {
                    adapter.Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
                }
            }
        }

        public void ConfigureShowTypingMiddleWare(BotFrameworkAdapter adapter, BotSettings settings)
        {
            if (settings.Feature.UseShowTypingMiddleware)
            {
                adapter.Use(new ShowTypingMiddleware());
            }
        }

        public void ConfigureInspectionMiddleWare(BotFrameworkAdapter adapter, BotSettings settings, IStorage storage)
        {
            if (settings.Feature.UseInspectionMiddleware)
            {
                adapter.Use(new InspectionMiddleware(new InspectionState(storage)));
            }
        }

        public IStorage ConfigureStorage(BotSettings settings)
        {
            IStorage storage;
            if (settings.Feature.UseCosmosDbPersistentStorage && !string.IsNullOrEmpty(settings.CosmosDb.AuthKey))
            {
                storage = new CosmosDbPartitionedStorage(settings.CosmosDb);
            }
            else
            {
                storage = new MemoryStorage();
            }

            return storage;
        }

        public BotFrameworkHttpAdapter GetBotAdapter(IStorage storage, BotSettings settings, UserState userState, ConversationState conversationState, IServiceProvider s)
        {
            HostContext.Current.Set<IConfiguration>(Configuration);

            var adapter = new BotFrameworkHttpAdapter(new ConfigurationCredentialProvider(this.Configuration));

            adapter
              .UseStorage(storage)
              .UseState(userState, conversationState);

            // Configure Middlewares
            ConfigureTranscriptLoggerMiddleware(adapter, settings);
            ConfigureInspectionMiddleWare(adapter, settings, storage);
            ConfigureShowTypingMiddleWare(adapter, settings);

            adapter.OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(exception.Message).ConfigureAwait(false);
                await conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
                await conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
            };
            return adapter;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton<IConfiguration>(this.Configuration);

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddSingleton<BotAdapter>(sp => (BotFrameworkHttpAdapter)sp.GetService<IBotFrameworkHttpAdapter>());

            // Register AuthConfiguration to enable custom claim validation.
            services.AddSingleton<AuthenticationConfiguration>();

            // Register the skills client and skills request handler.
            services.AddSingleton<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
            services.AddHttpClient<BotFrameworkClient, SkillHttpClient>();
            services.AddSingleton<ChannelServiceHandler, SkillHandler>();

            // Register telemetry client, initializers and middleware
            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();
            services.AddSingleton<TelemetryLoggerMiddleware>(sp =>
            {
                var telemetryClient = sp.GetService<IBotTelemetryClient>();
                return new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: settings.Telemetry.LogPersonalInformation);
            });
            services.AddSingleton<TelemetryInitializerMiddleware>(sp =>
            {
                var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                var telemetryLoggerMiddleware = sp.GetService<TelemetryLoggerMiddleware>();
                return new TelemetryInitializerMiddleware(httpContextAccessor, telemetryLoggerMiddleware, settings.Telemetry.LogActivities);
            });

            IStorage storage = ConfigureStorage(settings);

            services.AddSingleton(storage);
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);
            services.AddSingleton(userState);
            services.AddSingleton(conversationState);

            // Configure bot loading path
            var botDir = settings.Bot;
            var resourceExplorer = new ResourceExplorer().AddFolder(botDir);
            var rootDialog = GetRootDialog(botDir);

            var defaultLocale = Configuration.GetValue<string>("defaultLocale") ?? "en-us";

            services.AddSingleton(resourceExplorer);

            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>((s) => GetBotAdapter(storage, settings, userState, conversationState, s));

            services.AddSingleton<IBot>(s =>
                new ComposerBot(
                    s.GetService<ConversationState>(),
                    s.GetService<UserState>(),
                    s.GetService<ResourceExplorer>(),
                    s.GetService<BotFrameworkClient>(),
                    s.GetService<SkillConversationIdFactoryBase>(),
                    s.GetService<IBotTelemetryClient>(),
                    rootDialog,
                    defaultLocale));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseRouting()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapControllers();
               });
        }

        private string GetRootDialog(string folderPath)
        {
            var dir = new DirectoryInfo(folderPath);
            foreach (var f in dir.GetFiles())
            {
                if (f.Extension == ".dialog")
                {
                    return f.Name;
                }
            }

            throw new Exception($"Can't locate root dialog in {dir.FullName}");
        }
    }
}
