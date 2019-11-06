// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using PointOfInterestSkill.Services;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class FindPointOfInterestUtterances : BaseTestUtterances
    {
        public FindPointOfInterestUtterances()
        {
            this.Add(WhatsNearby, CreateIntent(WhatsNearby, PointOfInterestLuis.Intent.FindPointOfInterest));
            this.Add(FindNearestPoi, CreateIntent(FindNearestPoi, PointOfInterestLuis.Intent.FindPointOfInterest, poiType: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
        }

        public static string WhatsNearby { get; } = "What's nearby?";

        public static string FindNearestPoi { get; } = "find closest poi nearby me";
    }
}
