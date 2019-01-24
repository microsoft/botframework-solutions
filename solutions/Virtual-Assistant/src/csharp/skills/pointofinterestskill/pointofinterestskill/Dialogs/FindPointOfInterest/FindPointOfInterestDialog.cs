using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Route;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill.Dialogs.FindPointOfInterest
{
    public class FindPointOfInterestDialog : PointOfInterestSkillDialog
    {
        public FindPointOfInterestDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindPointOfInterestDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
                ResponseToGetRoutePrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.FindPointOfInterest, findPointOfInterest) { TelemetryClient = telemetryClient });
            AddDialog(new RouteDialog(services, Accessor, ServiceManager, TelemetryClient));

            // Set starting dialog for component
            InitialDialogId = Action.FindPointOfInterest;
        }
    }
}