// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PointOfInterestSkill
{
    using System;
    using System.Linq;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.AI.Luis;
    using Microsoft.Bot.Builder.BotFramework;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Integration;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Solutions.Middleware;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets application Configuration.
        /// </summary>
        /// <value>
        /// Application Configuration.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Service Collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions must be configured prior to setting up the State Accessors");
                }

                var conversationState = options.State.OfType<ConversationState>().FirstOrDefault();
                if (conversationState == null)
                {
                    throw new InvalidOperationException("ConversationState must be defined and added before adding conversation-scoped state accessors.");
                }

                var accessors = new PointOfInterestSkillAccessors
                {
                    ConversationDialogState = conversationState.CreateProperty<DialogState>("PointOfInterestSkillDialogState"),
                    PointOfInterestSkillState = conversationState.CreateProperty<PointOfInterestSkillState>("PointOfInterestSkillState"),
                };

                return accessors;
            });

            services.AddSingleton<PointOfInterestSkillServices>(sp =>
            {
                var pointOfInterestSkillService = new PointOfInterestSkillServices();
                var luisModels = this.Configuration.GetSection("services").Get<LanguageModel[]>();
                var luis = luisModels[0];
                var luisApp = new LuisApplication(luis.Id, luis.SubscriptionKey, "https://westus.api.cognitive.microsoft.com");
                pointOfInterestSkillService.LuisRecognizer = new LuisRecognizer(luisApp, null, true);
                return pointOfInterestSkillService;
            });

            services.AddSingleton<IServiceManager, ServiceManager>();

            services.AddBot<PointOfInterestSkill>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);

                // Catches any errors that occur during a conversation turn and logs them to AppInsights.
                options.OnTurnError = async (context, exception) =>
                {
                    await context.SendActivityAsync($"PointOfInterestSkill: {exception.Message}");
                    await context.SendActivityAsync(exception.StackTrace);
                };

                IStorage dataStore = new MemoryStorage();
                options.State.Add(new ConversationState(dataStore));
                options.Middleware.Add(new EventDebuggerMiddleware());
                options.Middleware.Add(new AutoSaveStateMiddleware(options.State.ToArray()));
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}