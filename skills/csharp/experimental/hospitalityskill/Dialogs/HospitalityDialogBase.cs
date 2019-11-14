﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace HospitalitySkill.Dialogs
{
    public class HospitalityDialogBase : ComponentDialog
    {
        public HospitalityDialogBase(
             string dialogId,
             BotSettings settings,
             BotServices services,
             ResponseManager responseManager,
             ConversationState conversationState,
             UserState userState,
             IHotelService hotelService,
             IBotTelemetryClient telemetryClient)
             : base(dialogId)
        {
            Settings = settings;
            Services = services;
            ResponseManager = responseManager;
            StateAccessor = conversationState.CreateProperty<HospitalitySkillState>(nameof(HospitalitySkillState));
            UserStateAccessor = userState.CreateProperty<HospitalityUserSkillState>(nameof(HospitalityUserSkillState));
            TelemetryClient = telemetryClient;
            HotelService = hotelService;

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

        protected IStatePropertyAccessor<HospitalitySkillState> StateAccessor { get; set; }

        protected IStatePropertyAccessor<HospitalityUserSkillState> UserStateAccessor { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        protected IHotelService HotelService { get; set; }

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
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {
                    return await sc.NextAsync(providerTokenResponse);
                }
                else
                {
                    return await sc.NextAsync();
                }
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
                var state = await StateAccessor.GetAsync(dc.Context, () => new HospitalitySkillState());
                state.LuisResult = dc.Context.TurnState.Get<HospitalityLuis>(StateProperties.SkillLuisResult);
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

        protected async Task<DialogTurnResult> HasCheckedOut(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));

            // if user has already checked out shouldn't be able to do anything else
            if (userState.CheckedOut)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.HasCheckedOut));
                return await sc.EndDialogAsync();
            }

            return await sc.NextAsync();
        }

        // Get card that renders for adaptive card 1.0
        protected string GetCardName(ITurnContext context, string name)
        {
            if (Channel.GetChannelId(context) == Channels.Msteams)
            {
                name += ".1.0";
            }

            return name;
        }
    }
}