using Luis;
using PointOfInterestSkill.Services;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class FindPointOfInterestUtterances : BaseTestUtterances
    {
        public FindPointOfInterestUtterances()
        {
            this.Add(WhatsNearby, CreateIntent(WhatsNearby, PointOfInterestLuis.Intent.NAVIGATION_FIND_POINTOFINTEREST));
            this.Add(FindNearestPoi, CreateIntent(FindNearestPoi, PointOfInterestLuis.Intent.NAVIGATION_FIND_POINTOFINTEREST, poiType: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
        }

        public static string WhatsNearby { get; } = "What's nearby?";

        public static string FindNearestPoi { get; } = "find closest poi nearby me";
    }
}
