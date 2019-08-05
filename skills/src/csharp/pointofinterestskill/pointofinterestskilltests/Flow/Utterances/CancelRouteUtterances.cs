using Luis;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class CancelRouteUtterances : BaseTestUtterances
    {
        public CancelRouteUtterances()
        {
            this.Add(CancelRoute, CreateIntent(CancelRoute, Luis.PointOfInterestLuis.Intent.NAVIGATION_CANCEL_ROUTE));
        }

        public static string CancelRoute { get; } = "cancel my route";
    }
}
