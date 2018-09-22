// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DemoSkill
{
    public class Startup
    {
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
            var botFilePath = Configuration.GetSection("botFilePath").Value;
            var botFileSecret = Configuration.GetSection("botFileSecret").Value;

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            var botConfig = BotConfiguration.LoadAsync(botFilePath).GetAwaiter().GetResult();
            services.AddSingleton(sp => botConfig);

            // Initializes your bot service clients and adds a singleton that your Bot can access through dependency injection.
            var connectedServices = InitBotServices(botConfig);
            services.AddSingleton(sp => connectedServices);

            // Initializes bot conversation and user state accessors
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;

                var conversationState = options.State.OfType<ConversationState>().FirstOrDefault();

                var accessors = new DemoSkillAccessors
                {
                    ConversationDialogState = conversationState.CreateProperty<DialogState>("DemoSkillDialogState"),
                    DemoSkillState = conversationState.CreateProperty<DemoSkillState>("DemoSkillState"),
                };

                return accessors;
            });

            // Initialize your MainDialog. This prevents your dialogs from being initialized on every turn.
            // services.AddSingleton(sp => new MainDialog(connectedServices, ) as Dialog);
            services.AddBot<DemoSkill>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);

                IStorage datastore = new CosmosDbStorage(connectedServices.CosmosDbOptions);
                options.State.Add(new ConversationState(datastore));
                options.Middleware.Add(new AutoSaveStateMiddleware(options.State.ToArray()));
                options.Middleware.Add(new EventDebuggerMiddleware());
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

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }

        /// <summary>
        /// Initializes service clients which will be used throughout the bot code into a single object.
        /// It is recommended that you add any additional service clients you may need into the <see cref="DemoSkillServices"/> object and initialize them here.
        /// These services include AppInsights telemetry client, Luis Recognizers, QnAMaker instances, etc.</summary>
        /// <param name="config">Bot configuration object based on .bot json file.</param>
        /// <returns>BotServices object.</returns>
        private DemoSkillServices InitBotServices(BotConfiguration config)
        {
            var connectedServices = new DemoSkillServices();

            foreach (var service in config.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.Generic:
                        {
                            // update readme with .bot update instructions
                            if (service.Name == "Authentication")
                            {
                                var authentication = service as GenericService;
                                connectedServices.AuthConnectionName = authentication.Configuration["Azure Active Directory v2"];
                            }

                            break;
                        }
                }
            }

            return connectedServices;
        }
    }
}
