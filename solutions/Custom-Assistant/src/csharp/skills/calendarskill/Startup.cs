// <copyright file="Startup.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Linq;
using CalendarSkill.Models;
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

namespace CalendarSkill
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

                var accessors = new CalendarSkillAccessors
                {
                    ConversationDialogState = conversationState.CreateProperty<DialogState>("CalendarSkillDialogState"),
                    CalendarSkillState = conversationState.CreateProperty<CalendarSkillState>("CalendarSkillState"),
                };

                return accessors;
            });

            services.AddSingleton<IServiceManager, ServiceManager>();

            services.AddSingleton<CalendarSkillServices>(sp =>
            {
                var luisModels = this.Configuration.GetSection("services").Get<LanguageModel[]>();

                var luis = luisModels[0];
                var calendarSkillService = new CalendarSkillServices();
                {
                    var luisApp = new LuisApplication(luis.Id, luis.SubscriptionKey, "https://westus.api.cognitive.microsoft.com");
                    var luisRecognizer = new LuisRecognizer(luisApp);
                    calendarSkillService.LuisRecognizer = luisRecognizer;
                    var authConnectionNmae = this.Configuration.GetSection("authConnectionName")?.Value;
                    calendarSkillService.AuthConnectionName = authConnectionNmae;
                }

                return calendarSkillService;
            });

            services.AddBot<CalendarSkill>(options =>
            {
                options.CredentialProvider = new ConfigurationCredentialProvider(Configuration);

                // Catches any errors that occur during a conversation turn and logs them to AppInsights.
                options.OnTurnError = async (context, exception) =>
                {
                    await context.SendActivityAsync($"CalendarSkill: {exception.Message}");
                    await context.SendActivityAsync(exception.StackTrace);
                };

                var transcriptStore = new AzureBlobTranscriptStore(this.Configuration.GetSection("AzureBlobConnectionString")?.Value, this.Configuration.GetSection("transcriptContainer")?.Value);
                options.Middleware.Add(new TranscriptLoggerMiddleware(transcriptStore));

                IStorage dataStore = new MemoryStorage();
                options.State.Add(new ConversationState(dataStore));
                options.Middleware.Add(new AutoSaveStateMiddleware(options.State.ToArray()));
            });
        }

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