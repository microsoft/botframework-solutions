using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.CancelRoute.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace PointOfInterestSkill
{
    public class CancelRouteDialog : PointOfInterestSkillDialog
    {
        public CancelRouteDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(CancelRouteDialog), services, accessor, serviceManager)
        {
            var cancelRoute = new WaterfallStep[]
            {
                CancelActiveRoute,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.CancelActiveRoute, cancelRoute));

            // Set starting dialog for component
            InitialDialogId = Action.CancelActiveRoute;
        }

        public async Task<DialogTurnResult> CancelActiveRoute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    var replyMessage = sc.Context.Activity.CreateReply(CancelRouteResponses.CancelActiveRoute, _responseBuilder);
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.ActiveRoute = null;
                    state.ActiveLocation = null;
                }
                else
                {
                    var replyMessage = sc.Context.Activity.CreateReply(CancelRouteResponses.CannotCancelActiveRoute, _responseBuilder);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

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
