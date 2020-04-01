// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton<IConfiguration>(this.Configuration);

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddSingleton<BotAdapter>(sp => (BotFrameworkHttpAdapter)sp.GetService<IBotFrameworkHttpAdapter>());

            // Register AuthConfiguration to enable custom claim validation.
            services.AddSingleton<AuthenticationConfiguration>();

            // Register the skills client and skills request handler.
            services.AddSingleton<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
            services.AddHttpClient<BotFrameworkClient, SkillHttpClient>();
            services.AddSingleton<ChannelServiceHandler, SkillHandler>();

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);

            IStorage storage = null;

            // Configure storage for deployment
            if (!string.IsNullOrEmpty(settings.CosmosDb.AuthKey))
            {
                storage = new CosmosDbStorage(settings.CosmosDb);
            }
            else
            {
                Console.WriteLine("The settings of CosmosDbStorage is incomplete, please check following settings: settings.CosmosDb");
                storage = new MemoryStorage();
            }

            services.AddSingleton(storage);
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);

            var botFile = Configuration.GetSection("bot").Get<string>();

            // manage all bot resources
            var resourceExplorer = new ResourceExplorer().AddFolder(botFile);

            services.AddSingleton(userState);
            services.AddSingleton(conversationState);
            services.AddSingleton(resourceExplorer);

            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>((s) =>
            {
                HostContext.Current.Set<IConfiguration>(Configuration);

                var adapter = new BotFrameworkHttpAdapter(new ConfigurationCredentialProvider(this.Configuration));
                adapter
                  .UseStorage(storage)
                  .UseState(userState, conversationState);               

                if (!string.IsNullOrEmpty(settings.BlobStorage.ConnectionString) && !string.IsNullOrEmpty(settings.BlobStorage.Container))
                {
                    adapter.Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
                }
                else
                {
                    Console.WriteLine("The settings of TranscriptLoggerMiddleware is incomplete, please check following settings: settings.BlobStorage.ConnectionString, settings.BlobStorage.Container");
                }

                adapter.OnTurnError = async (turnContext, exception) =>
                {
                    await turnContext.SendActivityAsync(exception.Message).ConfigureAwait(false);
                    await conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
                    await conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
                };
                return adapter;
            });

            services.AddSingleton<IBot, ComposerBot>();
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
    }
}
