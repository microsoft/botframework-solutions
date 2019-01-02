// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using NewsSkill.Dialogs.Main.Resources;

namespace NewsSkill
{
    public class NewsDialog : ComponentDialog
    {
        protected const string LuisResultKey = "LuisResult";

        public NewsDialog(
                   string dialogId,
                   SkillConfigurationBase services,
                   IStatePropertyAccessor<NewsSkillState> accessor,
                   IBotTelemetryClient telemetryClient)
                   : base(dialogId)
        {
            Services = services;
            Accessor = accessor;
            TelemetryClient = telemetryClient;
        }

        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<NewsSkillState> Accessor { get; set; }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        public async Task<Exception> HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(MainStrings.ERROR));

            await sc.CancelAllDialogsAsync();

            return ex;
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);

            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);

            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }
    }
}