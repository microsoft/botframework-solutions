// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Tests.Flow.Strings;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class FindParkingUtterances : BaseTestUtterances
    {
        public FindParkingUtterances()
        {
            this.Add(FindParkingNearby, CreateIntent(FindParkingNearby, PointOfInterestLuis.Intent.FindParking));
            this.Add(FindParkingNearest, CreateIntent(FindParkingNearest, PointOfInterestLuis.Intent.FindParking, poiDescription: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
            this.Add(FindParkingNearAddress, CreateIntent(FindParkingNearAddress, PointOfInterestLuis.Intent.FindParking, address: new string[] { ContextStrings.Ave }));
        }

        public static string FindParkingNearby { get; } = "find a parking garage";

        public static string FindParkingNearest { get; } = "find a nearest parking garage";

        public static string FindParkingNearAddress { get; } = $"find a parking garage near {ContextStrings.Ave}";
    }
}
