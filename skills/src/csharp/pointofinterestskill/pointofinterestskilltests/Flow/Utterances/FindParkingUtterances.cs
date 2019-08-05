using Luis;
using PointOfInterestSkillTests.Flow.Strings;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class FindParkingUtterances : BaseTestUtterances
    {
        public FindParkingUtterances()
        {
            this.Add(FindParkingNearby, CreateIntent(FindParkingNearby, PointOfInterestLuis.Intent.NAVIGATION_FIND_PARKING));
            this.Add(FindParkingNearAddress, CreateIntent(FindParkingNearAddress, PointOfInterestLuis.Intent.NAVIGATION_FIND_PARKING, keyword: new string[] { ContextStrings.Ave }));
        }

        public static string FindParkingNearby { get; } = "find a parking garage";

        public static string FindParkingNearAddress { get; } = $"find a parking garage near {ContextStrings.Ave}";
    }
}
