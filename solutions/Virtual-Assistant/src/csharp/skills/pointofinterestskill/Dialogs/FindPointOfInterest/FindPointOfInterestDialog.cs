using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;

namespace PointOfInterestSkill
{
    public class FindPointOfInterestDialog : PointOfInterestSkillDialog
    {
        public FindPointOfInterestDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(FindPointOfInterestDialog), services, accessor, serviceManager)
        {
            var findPointOfInterest = new WaterfallStep[]
          {
                GetPointOfInterestLocations,
                ResponseToGetRoutePrompt,
          };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.FindPointOfInterest, findPointOfInterest));
            AddDialog(new RouteDialog(services, Accessor, ServiceManager));

            // Set starting dialog for component
            InitialDialogId = Action.FindPointOfInterest;
        }
    }
}
