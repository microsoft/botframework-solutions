// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Tests.Flow.Strings;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class RouteFromXToYUtterances : BaseTestUtterances
    {
        public RouteFromXToYUtterances()
        {
            this.Add(FindRoute, CreateIntent(FindRoute, PointOfInterestLuis.Intent.GetDirections));
            this.Add(GetToMicrosoft, CreateIntent(GetToMicrosoft, PointOfInterestLuis.Intent.GetDirections, keyword: new string[] { ContextStrings.MicrosoftCorporation }));
            this.Add(GetToNearestPharmacy, CreateIntent(GetToNearestPharmacy, PointOfInterestLuis.Intent.GetDirections, keyword: new string[] { ContextStrings.Pharmacy }, poiDescription: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
        }

        public static string FindRoute { get; } = "find a route";

        public static string GetToMicrosoft { get; } = $"get directions to {ContextStrings.MicrosoftCorporation}";

        public static string GetToNearestPharmacy { get; } = $"get directions the nearest {ContextStrings.Pharmacy}";
    }
}
