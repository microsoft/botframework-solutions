// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Microsoft.Bot.Schema;
using Microsoft.CognitiveServices.ContentModerator.Models;
using Newtonsoft.Json.Linq;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Tests.Flow.Strings;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class RouteFromXToYUtterances : BaseTestUtterances
    {
        public static readonly string FindRoute = "find a route";

        public static readonly string GetToMicrosoft = $"get directions to {ContextStrings.MicrosoftCorporation}";

        public static readonly string GetToMicrosoftNearAddress = $"get directions to {ContextStrings.MicrosoftCorporation} near {ContextStrings.Ave}";

        public static readonly string GetToNearestPharmacy = $"get directions the nearest {ContextStrings.Pharmacy}";

        public static readonly Activity FindRouteAction = new Activity(type: ActivityTypes.Event, name: "GetDirectionAction", value: JObject.FromObject(new
        {
            currentLatitude = LocationLatitude,
            currentLongitude = LocationLongitude,
        }));

        public static readonly Activity GetToNearestPharmacyNoCurrentAction = new Activity(type: ActivityTypes.Event, name: "GetDirectionAction", value: JObject.FromObject(new
        {
            keyword = ContextStrings.Pharmacy,
            poiType = GeoSpatialServiceTypes.PoiType.Nearest,
        }));

        public RouteFromXToYUtterances()
        {
            this.Add(FindRoute, CreateIntent(FindRoute, PointOfInterestLuis.Intent.GetDirections));
            this.Add(GetToMicrosoft, CreateIntent(GetToMicrosoft, PointOfInterestLuis.Intent.GetDirections, keyword: new string[] { ContextStrings.MicrosoftCorporation }));
            this.Add(GetToMicrosoftNearAddress, CreateIntent(GetToMicrosoftNearAddress, PointOfInterestLuis.Intent.GetDirections, keyword: new string[] { ContextStrings.MicrosoftCorporation }, address: new string[] { ContextStrings.Ave }));
            this.Add(GetToNearestPharmacy, CreateIntent(GetToNearestPharmacy, PointOfInterestLuis.Intent.GetDirections, keyword: new string[] { ContextStrings.Pharmacy }, poiDescription: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
        }
    }
}
