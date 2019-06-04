// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AdaptiveAssistant.Bots;
using AdaptiveAssistant.Dialogs;
using AdaptiveAssistant.Services;
using System.IO;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace AdaptiveAssistant
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

            var provider = services.BuildServiceProvider();

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);
            services.AddSingleton(settings);

            // Configure bot services
            services.AddSingleton<BotServices>();

            // Configure credentials
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddSingleton(new MicrosoftAppCredentials(settings.MicrosoftAppId, settings.MicrosoftAppPassword));

            // Configure telemetry
            var telemetryClient = new BotTelemetryClient(new TelemetryClient(settings.AppInsights));
            services.AddSingleton<IBotTelemetryClient>(telemetryClient);
            services.AddBotApplicationInsights(telemetryClient);

            // Configure storage
            // services.AddSingleton<IStorage>(new CosmosDbStorage(settings.CosmosDb));
            services.AddSingleton<IStorage>(new MemoryStorage());
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();

            // Register resource explorer for language generation
            services.AddSingleton(sp => ResourceExplorer.LoadProject(Directory.GetCurrentDirectory()));

            services.AddSingleton(sp => TemplateEngine.FromFiles(
                "./Responses/MainResponses.lg",
                "./Responses/EscalateResponses.lg",
                "./Responses/CancelResponses.lg",
                "./Responses/OnboardingResponses.lg"));

            // Configure adapters
            services.AddSingleton<IBotFrameworkHttpAdapter, DefaultAdapter>();

            // Register dialogs
            services.AddTransient<AdaptiveMainDialog>();

            // Configure bot
            services.AddTransient<IBot, DialogBot<AdaptiveMainDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseBotApplicationInsights()
                .UseDefaultFiles()
                .UseStaticFiles()
                .UseMvc();
        }

        // This method creates a MultiProviderAuthDialog based on a skill manifest.
        private MultiProviderAuthDialog BuildAuthDialog(SkillManifest skill, BotSettings settings)
        {
            if (skill.AuthenticationConnections?.Count() > 0)
            {
                if (settings.OAuthConnections.Any() && settings.OAuthConnections.Any(o => skill.AuthenticationConnections.Any(s => s.ServiceProviderId == o.Provider)))
                {
                    var oauthConnections = settings.OAuthConnections.Where(o => skill.AuthenticationConnections.Any(s => s.ServiceProviderId == o.Provider)).ToList();
                    return new MultiProviderAuthDialog(oauthConnections);
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