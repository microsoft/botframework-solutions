// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveAssistant.Adapters;
using AdaptiveAssistant.Bots;
using AdaptiveAssistant.Dialogs;
using AdaptiveAssistant.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.StreamingExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            HostingEnvironment = env;
        }

        public IHostingEnvironment HostingEnvironment { get; set; }

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
            var telemetryClient = new BotTelemetryClient(new TelemetryClient());
            services.AddSingleton<IBotTelemetryClient>(telemetryClient);
            services.AddBotApplicationInsights(telemetryClient);

            // Configure bot services
            services.AddSingleton<BotServices>();

            // Configure storage
            services.AddSingleton<IStorage>(new CosmosDbStorage(settings.CosmosDb));
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();

            // var resourceExplorer = ResourceExplorer.LoadProject(HostingEnvironment.ContentRootPath);
            var resourceExplorer = new ResourceExplorer().AddFolder(Directory.GetCurrentDirectory());
            services.AddSingleton(resourceExplorer);

            services.AddSingleton(sp => new TemplateEngine().AddFiles(new[]
            {
                "./Responses/MainResponses.lg",
                "./Responses/OnboardingResponses.lg",
            }));

            // Register skill dialogs
            services.AddTransient(sp =>
            {
                var userState = sp.GetService<UserState>();
                var skillDialogs = new List<SkillDialog>();

                foreach (var skill in settings.Skills)
                {
                    var authDialog = BuildAuthDialog(skill, settings, appCredentials);
                    var credentials = new MicrosoftAppCredentialsEx(settings.MicrosoftAppId, settings.MicrosoftAppPassword, skill.MSAappId);
                    skillDialogs.Add(new SkillDialog(skill, credentials, telemetryClient, userState, authDialog));
                }

                return skillDialogs;
            });

            // Register dialogs
            services.AddSingleton<GeneralDialog>();
            services.AddSingleton<OnboardingDialog>();
            services.AddSingleton<DispatchDialog>();

            // Configure adapters
            services.AddSingleton<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddSingleton<WebSocketEnabledHttpAdapter, DefaultWebSocketAdapter>();

            // Configure bot
            services.AddSingleton<IBot, DialogBot<DispatchDialog>>();
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