// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VirtualAssistantSample.Adapters;
using VirtualAssistantSample.Bots;
using VirtualAssistantSample.Dialogs;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);
            services.AddSingleton(settings);

            // Configure credentials
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            var appCredentials = new MicrosoftAppCredentials(settings.MicrosoftAppId, settings.MicrosoftAppPassword);
            services.AddSingleton(appCredentials);

            // Configure telemetry
            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();
            services.AddSingleton<TelemetryInitializerMiddleware>();
            services.AddSingleton<TelemetryLoggerMiddleware>();

            // Configure bot services
            services.AddSingleton<BotServices>();

            // Configure storage
            // Uncomment the following line for local development without Cosmos Db
            // services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton<IStorage>(new CosmosDbStorage(settings.CosmosDb));
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();

            // Base template supports 6 languages out of the box and two LG templates are provided.
            Dictionary<string, List<string>> localeLGFiles = new Dictionary<string, List<string>>();
            List<string> languageGenerationTemplateFiles = new List<string>() { "MainResponses", "OnboardingResponses" };
            List<string> supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };

            foreach (var locale in supportedLocales)
            {
                var templateFiles = new List<string>();
                foreach (var template in languageGenerationTemplateFiles)
                {
                    // EN doesn't have a locale suffix response file.
                    if (locale.StartsWith("en"))
                    {
                        templateFiles.Add(Path.Combine(".", "Responses", $"{template}.lg"));
                    }
                    else
                    {
                        templateFiles.Add(Path.Combine(".", "Responses", $"{template}.{locale}.lg"));
                    }
                }

                localeLGFiles.Add(locale, templateFiles);
            }

            services.AddSingleton(new LocaleTemplateEngineManager(localeLGFiles, settings.DefaultLocale ?? "en-us"));

            // Register dialogs
            services.AddTransient<MainDialog>();
            services.AddTransient<OnboardingDialog>();

            // Register skill dialogs
            foreach (var skill in settings.Skills)
            {
                var authDialog = BuildAuthDialog(skill, settings, appCredentials);
                var credentials = new MicrosoftAppCredentialsEx(settings.MicrosoftAppId, settings.MicrosoftAppPassword, skill.MSAappId);
                services.AddTransient(sp =>
                {
                    var userState = sp.GetService<UserState>();
                    var telemetryClient = sp.GetService<IBotTelemetryClient>();
                    return new SkillDialog(skill, credentials, telemetryClient, userState, authDialog);
                });
            }

            // IBotFrameworkHttpAdapter now supports both http and websocket transport
            services.AddSingleton<IBotFrameworkHttpAdapter, DefaultAdapter>();

            // Configure bot
            services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseMvc();
        }

        // This method creates a MultiProviderAuthDialog based on a skill manifest.
        private MultiProviderAuthDialog BuildAuthDialog(SkillManifest skill, BotSettings settings, MicrosoftAppCredentials appCredentials)
        {
            if (skill.AuthenticationConnections?.Count() > 0)
            {
                if (settings.OAuthConnections != null && settings.OAuthConnections.Any(o => skill.AuthenticationConnections.Any(s => s.ServiceProviderId == o.Provider)))
                {
                    var oauthConnections = settings.OAuthConnections.Where(o => skill.AuthenticationConnections.Any(s => s.ServiceProviderId == o.Provider)).ToList();
                    return new MultiProviderAuthDialog(oauthConnections, appCredentials);
                }
                else
                {
                    throw new Exception($"You must configure at least one supported OAuth connection to use this skill: {skill.Name}.");
                }
            }

            return null;
        }
    }
}