using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.CancelRoute.Resources;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill.Dialogs.CancelRoute
{
    public class CancelRouteDialog : PointOfInterestSkillDialog
    {
        public CancelRouteDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(CancelRouteDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var cancelRoute = new WaterfallStep[]
            {
                CancelActiveRoute,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.CancelActiveRoute, cancelRoute) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Action.CancelActiveRoute;
        }

        public async Task<DialogTurnResult> CancelActiveRoute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    var replyMessage = sc.Context.Activity.CreateReply(CancelRouteResponses.CancelActiveRoute, ResponseBuilder);
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.ActiveRoute = null;
                    state.ActiveLocation = null;
                }
                else
                {
                    var replyMessage = sc.Context.Activity.CreateReply(CancelRouteResponses.CannotCancelActiveRoute, ResponseBuilder);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync();
            }
            catch
            {
                await HandleDialogException(sc);
                throw;
            }
        }
    }
}