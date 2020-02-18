// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Tests.Flow.Strings;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class FindPointOfInterestUtterances : BaseTestUtterances
    {
        public static readonly string WhatsNearby = "What's nearby?";

        public static readonly string FindNearestPoi = "find closest poi nearby me";

        public static readonly string FindPharmacy = $"find a {ContextStrings.Pharmacy} nearby me";

        public static readonly string FindPoiNearAddress = $"find poi near {ContextStrings.Ave}";

        public static readonly Activity WhatsNearbyAction = new Activity(type: ActivityTypes.Event, name: "FindPointOfInterestAction", value: JObject.FromObject(new
        {
            currentLatitude = LocationLatitude,
            currentLongitude = LocationLongitude,
        }));

        public static readonly Activity FindNearestPoiWithRouteAction = new Activity(type: ActivityTypes.Event, name: "FindPointOfInterestAction", value: JObject.FromObject(new
        {
            currentLatitude = LocationLatitude,
            currentLongitude = LocationLongitude,
            poiType = GeoSpatialServiceTypes.PoiType.Nearest,
            showRoute = true,
        }));

        public FindPointOfInterestUtterances()
        {
            this.Add(WhatsNearby, CreateIntent(WhatsNearby, PointOfInterestLuis.Intent.FindPointOfInterest));
            this.Add(FindNearestPoi, CreateIntent(FindNearestPoi, PointOfInterestLuis.Intent.FindPointOfInterest, poiDescription: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
            this.Add(FindPharmacy, CreateIntent(FindPharmacy, PointOfInterestLuis.Intent.FindPointOfInterest, keyword: new string[] { ContextStrings.Pharmacy }, keywordCategory: new string[][] { new string[] { "category" } }, categoryText: new string[] { ContextStrings.Pharmacy }));
            this.Add(FindPoiNearAddress, CreateIntent(FindPoiNearAddress, PointOfInterestLuis.Intent.FindPointOfInterest, address: new string[] { ContextStrings.Ave }));
        }
    }
}
