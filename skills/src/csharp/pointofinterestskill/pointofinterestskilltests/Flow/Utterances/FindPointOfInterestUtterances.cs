using Luis;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class FindPointOfInterestUtterances : BaseTestUtterances
    {
        public FindPointOfInterestUtterances()
        {
            this.Add(WhatsNearby, CreateIntent(WhatsNearby, PointOfInterestLuis.Intent.NAVIGATION_FIND_POINTOFINTEREST));
        }

        public static string WhatsNearby { get; } = "What's nearby?";
    }
}
