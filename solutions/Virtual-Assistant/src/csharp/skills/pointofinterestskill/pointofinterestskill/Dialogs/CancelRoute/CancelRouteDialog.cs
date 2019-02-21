// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Dialogs.CancelRoute.Resources;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill.Dialogs.CancelRoute
{
    public class CancelRouteDialog : PointOfInterestSkillDialog
    {
        public CancelRouteDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(CancelRouteDialog), services, responseManager, accessor, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var cancelRoute = new WaterfallStep[]
            {
                CancelActiveRoute,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CancelActiveRoute, cancelRoute) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.CancelActiveRoute;
        }

        public async Task<DialogTurnResult> CancelActiveRoute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    var replyMessage = ResponseManager.GetResponse(CancelRouteResponses.CancelActiveRoute);
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.ActiveRoute = null;
                    state.Destination = null;
                }
                else
                {
                    var replyMessage = ResponseManager.GetResponse(CancelRouteResponses.CannotCancelActiveRoute);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}