using Luis;
using PointOfInterestSkill.Services;
using PointOfInterestSkillTests.Flow.Strings;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class FindParkingUtterances : BaseTestUtterances
    {
        public FindParkingUtterances()
        {
            this.Add(FindParkingNearby, CreateIntent(FindParkingNearby, PointOfInterestLuis.Intent.FindParking));
            this.Add(FindParkingNearest, CreateIntent(FindParkingNearest, PointOfInterestLuis.Intent.FindParking, poiType: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
            this.Add(FindParkingNearAddress, CreateIntent(FindParkingNearAddress, PointOfInterestLuis.Intent.FindParking, keyword: new string[] { ContextStrings.Ave }));
        }

        public static string FindParkingNearby { get; } = "find a parking garage";

        public static string FindParkingNearest { get; } = "find a nearest parking garage";

        public static string FindParkingNearAddress { get; } = $"find a parking garage near {ContextStrings.Ave}";
    }
}
