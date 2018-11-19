// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using global::AutomotiveSkill.Dialogs.Shared.Resources;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Extensions;
    using Microsoft.Bot.Solutions.Skills;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class AutomotiveSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";
            
        public AutomotiveSkillDialog(
            string dialogId,
            SkillConfiguration services,
            IStatePropertyAccessor<AutomotiveSkillState> accessor,
            IServiceManager serviceManager)
            : base(dialogId)
        {
            Services = services;
            Accessor = accessor;
            ServiceManager = serviceManager;
        }

        protected SkillConfiguration Services { get; set; }

        protected IStatePropertyAccessor<AutomotiveSkillState> Accessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected AutomotiveSkillResponseBuilder ResponseBuilder { get; set; } = new AutomotiveSkillResponseBuilder();

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            await DigestLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);
            await DigestLuisResult(dc, state.LuisResult);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        public async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (AutomotiveSkillDialogOptions)sc.Options;

                // If in Skill mode we ask the calling Bot for the token
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
                    // TODO Error handling - if we get a new activity that isn't an event
                    var response = sc.Context.Activity.CreateReply();
                    response.Type = ActivityTypes.Event;
                    response.Name = "tokens/request";

                    // Send the tokens/request Event
                    await sc.Context.SendActivityAsync(response);

                    // Wait for the tokens/response event
                    return await sc.PromptAsync(SkillModeAuth, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(LocalModeAuth, new PromptOptions());
                }
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

     

        // Helpers
        public async Task DigestLuisResult(DialogContext dc, VehicleSettings luisResult)
        {
            try
            {
                var state = await Accessor.GetAsync(dc.Context);

                state.LuisResult = luisResult;

                // extract entities and store in state here.
            }
            catch
            {
                // put log here
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        public async Task<Exception> HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(AutomotiveSkillSharedResponses.ErrorMessage));
            await sc.CancelAllDialogsAsync();
            return ex;
        }
    }
}
