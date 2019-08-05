using Luis;
using PointOfInterestSkill.Services;
using PointOfInterestSkillTests.Flow.Strings;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class FindParkingUtterances : BaseTestUtterances
    {
        public FindParkingUtterances()
        {
            this.Add(FindParkingNearby, CreateIntent(FindParkingNearby, PointOfInterestLuis.Intent.NAVIGATION_FIND_PARKING));
            this.Add(FindParkingNearest, CreateIntent(FindParkingNearest, PointOfInterestLuis.Intent.NAVIGATION_FIND_PARKING, poiType: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
            this.Add(FindParkingNearAddress, CreateIntent(FindParkingNearAddress, PointOfInterestLuis.Intent.NAVIGATION_FIND_PARKING, keyword: new string[] { ContextStrings.Ave }));
        }

        public static string FindParkingNearby { get; } = "find a parking garage";

        public static string FindParkingNearest { get; } = "find a nearest parking garage";

        public static string FindParkingNearAddress { get; } = $"find a parking garage near {ContextStrings.Ave}";
    }
}
