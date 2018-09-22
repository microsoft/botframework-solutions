// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using EmailSkill.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmailSkill
{
    /// <summary>
    /// Bot App Startup.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940.
        /// </summary>
        /// <param name="env">Hosting Environment.</param>
        public Startup(IHostingEnvironment env)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
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
            // var botFilePath = Configuration.GetSection("BotFilePath")?.Value;
            // var botFileSecret = Configuration.GetSection("BotFileSecret")?.Value;

            //// Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            // var botConfig = BotConfiguration.LoadAsync(botFilePath).GetAwaiter().GetResult();
            // services.AddSingleton(sp => botConfig);

            //// Initializes your bot service clients and adds a singleton that your Bot can access through dependency injection.
            // var connectedServices = InitBotServices(botConfig);
            var luisModels = this.Configuration.GetSection("services").Get<LanguageModel[]>();

            var luis = luisModels[0];
            var emailSkillServices = new EmailSkillServices();
            {
                var luisApp = new LuisApplication(luis.Id, luis.SubscriptionKey, "https://westus.api.cognitive.microsoft.com");
                var luisRecognizer = new LuisRecognizer(luisApp);

                emailSkillServices.LuisRecognizer = luisRecognizer;
            }

            services.AddSingleton(sp => emailSkillServices);

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

                var accessors = new EmailSkillAccessors
                {
                    ConversationDialogState = conversationState.CreateProperty<DialogState>("EmailSkillDialogState"),
                    EmailSkillState = conversationState.CreateProperty<EmailSkillState>("EmailSkillState"),
                };

                return accessors;
            });

            services.AddSingleton<IMailSkillServiceManager, MailSkillServiceManager>();

            services.AddBot<EmailSkill>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(this.Configuration);

                var transcriptStore = new AzureBlobTranscriptStore(this.Configuration.GetSection("AzureBlobConnectionString")?.Value, this.Configuration.GetSection("transcriptContainer")?.Value);
                options.Middleware.Add(new TranscriptLoggerMiddleware(transcriptStore));
                IStorage memoryDataStore = new MemoryStorage();
                options.State.Add(new ConversationState(memoryDataStore));
                options.Middleware.Add(new AutoSaveStateMiddleware(options.State.ToArray()));
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
    }
}
