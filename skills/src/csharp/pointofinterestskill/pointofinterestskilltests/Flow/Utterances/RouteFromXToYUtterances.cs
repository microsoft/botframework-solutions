using Luis;
using PointOfInterestSkillTests.Flow.Strings;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class RouteFromXToYUtterances : BaseTestUtterances
    {
        public RouteFromXToYUtterances()
        {
            this.Add(FindRoute, CreateIntent(FindRoute, PointOfInterestLuis.Intent.NAVIGATION_ROUTE_FROM_X_TO_Y));
            this.Add(GetToMicrosoft, CreateIntent(GetToMicrosoft, PointOfInterestLuis.Intent.NAVIGATION_ROUTE_FROM_X_TO_Y, keyword: new string[] { ContextStrings.MicrosoftCorporation }));
            this.Add(GetToPharmacy, CreateIntent(GetToPharmacy, PointOfInterestLuis.Intent.NAVIGATION_ROUTE_FROM_X_TO_Y, keyword: new string[] { ContextStrings.Pharmacy }));
        }

        public static string FindRoute { get; } = "find a route";

        public static string GetToMicrosoft { get; } = $"get directions to {ContextStrings.MicrosoftCorporation}";

        public static string GetToPharmacy { get; } = $"get directions the {ContextStrings.Pharmacy}";
    }
}
