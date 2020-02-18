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
    public class FindParkingUtterances : BaseTestUtterances
    {
        public static readonly string FindParkingNearby = "find a parking garage";

        public static readonly string FindParkingNearest = "find a nearest parking garage";

        public static readonly string FindParkingNearAddress = $"find a parking garage near {ContextStrings.Ave}";

        public static readonly Activity FindParkingNearbyAction = new Activity(type: ActivityTypes.Event, name: "FindParkingAction", value: JObject.FromObject(new
        {
            currentLatitude = LocationLatitude,
            currentLongitude = LocationLongitude,
        }));

        public static readonly Activity FindParkingNearestNoRouteAction = new Activity(type: ActivityTypes.Event, name: "FindParkingAction", value: JObject.FromObject(new
        {
            currentLatitude = LocationLatitude,
            currentLongitude = LocationLongitude,
            poiType = GeoSpatialServiceTypes.PoiType.Nearest,
            showRoute = false,
        }));

        public FindParkingUtterances()
        {
            this.Add(FindParkingNearby, CreateIntent(FindParkingNearby, PointOfInterestLuis.Intent.FindParking));
            this.Add(FindParkingNearest, CreateIntent(FindParkingNearest, PointOfInterestLuis.Intent.FindParking, poiDescription: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
            this.Add(FindParkingNearAddress, CreateIntent(FindParkingNearAddress, PointOfInterestLuis.Intent.FindParking, address: new string[] { ContextStrings.Ave }));
        }
    }
}
