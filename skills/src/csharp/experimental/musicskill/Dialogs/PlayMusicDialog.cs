// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using MusicSkill.Responses.Sample;
using MusicSkill.Services;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums; 
using SpotifyAPI.Web.Models;

namespace MusicSkill.Dialogs
{
    public class PlayMusicDialog : SkillDialogBase
    {
        public PlayMusicDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(PlayMusicDialog), settings, services, responseManager, conversationState, telemetryClient)
        {
            var sample = new WaterfallStep[]
            {
                // NOTE: Uncomment these lines to include authentication steps to this dialog
                // GetAuthToken,
                // AfterGetAuthToken,
                PromptForName,
                GreetUser,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(PlayMusicDialog), sample));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(PlayMusicDialog);
        }

        private async Task<DialogTurnResult> PromptForName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // NOTE: Uncomment the following lines to access LUIS result for this turn.
            var state = await StateAccessor.GetAsync(stepContext.Context);
            var intent = state.LuisResult.TopIntent().intent;
            var entities = state.LuisResult.Entities;

            // Extract query entity to search against Spotify for
            var searchQuery = entities.Artist[0];

            // Get Spotify Client
            var client = await GetSpotifyWebAPIClient(Settings);

            var searchItems = await client.SearchItemsEscapedAsync(searchQuery, SearchType.Artist);

            var prompt = ResponseManager.GetResponse(SampleResponses.NamePrompt);
            return await stepContext.PromptAsync(DialogIds.NamePrompt, new PromptOptions { Prompt = prompt });
        }

        private async Task<DialogTurnResult> GreetUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokens = new StringDictionary
            {
                { "Name", stepContext.Result.ToString() },
            };

            var response = ResponseManager.GetResponse(SampleResponses.HaveNameMessage, tokens);
            await stepContext.Context.SendActivityAsync(response);

            return await stepContext.NextAsync();
        }

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }

        private async Task<SpotifyWebAPI> GetSpotifyWebAPIClient(BotSettings settings)
        {
            CredentialsAuth auth = new CredentialsAuth(settings.SpotifyClientId, settings.SpotifyClientSecret);
            Token token = await auth.GetToken();
            SpotifyWebAPI api = new SpotifyWebAPI() { TokenType = token.TokenType, AccessToken = token.AccessToken };
            return api;
        }
    }
}
