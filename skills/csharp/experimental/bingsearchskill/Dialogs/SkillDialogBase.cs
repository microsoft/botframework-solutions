// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BingSearchSkill.Models;
using BingSearchSkill.Responses.Shared;
using BingSearchSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Drawing.Drawing2D;

namespace BingSearchSkill.Dialogs
{
    public class SkillDialogBase : ComponentDialog
    {
        public SkillDialogBase(
             string dialogId,
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             IBotTelemetryClient telemetryClient)
             : base(dialogId)
        {
            Services = services;
            ResponseManager = responseManager;
            StateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            TelemetryClient = telemetryClient;

            // NOTE: Uncomment the following if your skill requires authentication
            // if (!Settings.OAuthConnections.Any())
            // {
            //     throw new Exception("You must configure an authentication connection before using this component.");
            // }
            //
            // AddDialog(new MultiProviderAuthDialog(services));
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<SkillState> StateAccessor { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await GetLuisResult(dc);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions());
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var providerTokenResponse = sc.Result as ProviderTokenResponse;

                if (providerTokenResponse != null)
                {
                    var state = await StateAccessor.GetAsync(sc.Context);
                    state.Token = providerTokenResponse.TokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Validators
        protected Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        // Helpers
        protected async Task GetLuisResult(DialogContext dc)
        {
            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                var state = await StateAccessor.GetAsync(dc.Context, () => new SkillState());

                // Get luis service for current locale
                var localeConfig = Services.GetCognitiveModels();
                var luisService = localeConfig.LuisServices["BingSearchSkill"];

                // Get intent and entities for activity
                var result = await luisService.RecognizeAsync<BingSearchSkillLuis>(dc.Context, CancellationToken.None);
                state.LuisResult = result;
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ErrorMessage));

            // clear state
            var state = await StateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }

        protected string ImageToDataUri(string imageUrl)
        {
            var httpClient = new HttpClient();

            long useDataUriJpegQuality = 75;

            if (useDataUriJpegQuality > 0 && !string.IsNullOrEmpty(imageUrl))
            {
                using (var image = Image.FromStream(httpClient.GetStreamAsync(imageUrl).Result))
                {
                    MemoryStream ms = new MemoryStream();
                    var encoder = ImageCodecInfo.GetImageDecoders().Where(x => x.FormatID == ImageFormat.Jpeg.Guid).FirstOrDefault();
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, useDataUriJpegQuality);
                    var maxWidth = 200;
                    if (image.Width > maxWidth)
                    {
                        var newWidth = maxWidth;
                        var newHeight = newWidth * image.Height / image.Width;
                        var res = new Bitmap(newWidth, newHeight);

                        using (var graphic = Graphics.FromImage(res))
                        {
                            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphic.SmoothingMode = SmoothingMode.HighQuality;
                            graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            graphic.CompositingQuality = CompositingQuality.HighQuality;
                            graphic.DrawImage(image, 0, 0, newWidth, newHeight);
                        }

                        res.Save(ms, encoder, encoderParameters);
                    }
                    else
                    {
                        image.Save(ms, encoder, encoderParameters);
                    }

                    return $"data:image/jpeg;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }

            return string.Empty;
        }

        private class DialogIds
        {
            public const string SkillModeAuth = "SkillAuth";
        }
    }
}