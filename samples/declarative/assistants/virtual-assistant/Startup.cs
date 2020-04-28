// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using VirtualAssistantSample.Adapters;
using VirtualAssistantSample.Bots;
using VirtualAssistantSample.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using VirtualAssistantSample.Services;
using Microsoft.Bot.Builder.Azure;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions.Skills;
using VirtualAssistantSample.Authentication;
using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using VirtualAssistantSample.TokenExchange;
using Microsoft.Bot.Builder.Dialogs;
using System;

namespace VirtualAssistantSample
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("cognitivemodels.json", optional: true)
                .AddJsonFile($"cognitivemodels.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton(Configuration);

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);
            services.AddSingleton(settings);

            // Configure channel provider
            services.AddSingleton<IChannelProvider, ConfigurationChannelProvider>();

            // Configure configuration provider
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            // Register the skills configuration class.
            var skillsConfig = new SkillsConfiguration(Configuration);
            services.AddSingleton(skillsConfig);

            // Register AuthConfiguration to enable custom claim validation.
            services.AddSingleton(sp => new AuthenticationConfiguration { ClaimsValidator = new AllowedCallersClaimsValidator(skillsConfig) });

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
            services.AddSingleton<IStorage>(new CosmosDbPartitionedStorage(settings.CosmosDb));
            services.AddSingleton<UserState>();
            services.AddSingleton<ConversationState>();

            // Configure localized responses
            var localizedTemplates = new Dictionary<string, string>();
            var templateFile = "AllResponses";
            var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };

            foreach (var locale in supportedLocales)
            {
                // LG template for en-us does not include locale in file extension.
                var localeTemplateFile = locale.Equals("en-us")
                    ? Path.Combine(".", "Responses", $"{templateFile}.lg")
                    : Path.Combine(".", "Responses", $"{templateFile}.{locale}.lg");

                localizedTemplates.Add(locale, localeTemplateFile);
            }

            services.AddSingleton(new LocaleTemplateManager(localizedTemplates, settings.DefaultLocale ?? "en-us"));

            // Register the Bot Framework Adapter with error handling enabled.
            // Note: some classes use the base BotAdapter so we add an extra registration that pulls the same instance.
            services.AddSingleton<IBotFrameworkHttpAdapter, DefaultAdapter>();
            services.AddSingleton<BotAdapter>(sp => sp.GetService<BotFrameworkHttpAdapter>());

            // Register the skills conversation ID factory, the client and the request handler.
            services.AddSingleton<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
            services.AddHttpClient<SkillHttpClient>();
            services.AddSingleton<ChannelServiceHandler, TokenExchangeSkillHandler>();

            // Register the SkillDialogs (remote skills).
            var botId = Configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
            if (string.IsNullOrWhiteSpace(botId))
            {
                throw new ArgumentException($"{MicrosoftAppCredentials.MicrosoftAppIdKey} is not in configuration");
            }

            foreach (var skill in skillsConfig.Skills.Values)
            {
                services.AddSingleton(sp =>
                {
                    var skillDialogOptions = new SkillDialogOptions
                    {
                        BotId = botId,
                        ConversationIdFactory = sp.GetService<SkillConversationIdFactoryBase>(),
                        SkillClient = sp.GetService<SkillHttpClient>(),
                        SkillHostEndpoint = skillsConfig.SkillHostEndpoint,
                        Skill = skill,
                        ConversationState = sp.GetService<ConversationState>()
                    };

                    return new SkillDialog(skillDialogOptions, skill.Id);
                });
            }

            // Configure TokenExchangeConfig for SSO
            if (settings.TokenExchangeConfig != null)
            {
                services.AddSingleton<ITokenExchangeConfig>(settings.TokenExchangeConfig);
            }

            // The Dialog that will be run by the bot.
            services.AddSingleton<RootDialog>();
            services.AddSingleton<ChitchatDialog>();
            services.AddSingleton<FaqDialog>();
            services.AddSingleton<GeneralDialog>();
            services.AddSingleton<OnboardingDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DefaultActivityHandler<RootDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
