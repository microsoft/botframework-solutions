// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using System;
    using System.Linq;
    using global::ToDoSkill.Models;
    using global::ToDoSkill.ServiceClients;
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

                var accessors = new ToDoSkillAccessors
                {
                    ConversationDialogState = conversationState.CreateProperty<DialogState>("ToDoSkillDialogState"),
                    ToDoSkillState = conversationState.CreateProperty<ToDoSkillState>("ToDoSkillState"),
                };

                return accessors;
            });

            services.AddSingleton<ToDoSkillServices>(sp =>
            {
                var toDoSkillService = new ToDoSkillServices();
                var luisModels = this.Configuration.GetSection("services").Get<LanguageModel[]>();
                var luis = luisModels[0];
                var luisApp = new LuisApplication(luis.Id, luis.SubscriptionKey, "https://westus.api.cognitive.microsoft.com");
                toDoSkillService.LuisRecognizer = new LuisRecognizer(luisApp, null, true);
                var authConnectionNmae = this.Configuration.GetSection("authConnectionName")?.Value;
                toDoSkillService.AuthConnectionName = authConnectionNmae;
                return toDoSkillService;
            });

            services.AddTransient<IToDoService, ToDoService>();

            services.AddBot<ToDoSkill>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(this.Configuration);

                // Catches any errors that occur during a conversation turn and logs them to AppInsights.
                options.OnTurnError = async (context, exception) =>
                {
                    await context.SendActivityAsync($"ToDoSkill: {exception.Message}");
                    await context.SendActivityAsync(exception.StackTrace);
                };

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
