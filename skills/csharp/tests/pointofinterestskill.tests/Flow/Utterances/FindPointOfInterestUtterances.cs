// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Tests.Flow.Strings;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class FindPointOfInterestUtterances : BaseTestUtterances
    {
        public FindPointOfInterestUtterances()
        {
            this.Add(WhatsNearby, CreateIntent(WhatsNearby, PointOfInterestLuis.Intent.FindPointOfInterest));
            this.Add(FindNearestPoi, CreateIntent(FindNearestPoi, PointOfInterestLuis.Intent.FindPointOfInterest, poiDescription: new string[][] { new string[] { GeoSpatialServiceTypes.PoiType.Nearest } }));
            this.Add(FindPharmacy, CreateIntent(FindPharmacy, PointOfInterestLuis.Intent.FindPointOfInterest, keyword: new string[] { ContextStrings.Pharmacy }, keywordCategory: new string[][] { new string[] { "category" } }, categoryText: new string[] { ContextStrings.Pharmacy }));
            this.Add(FindPoiNearAddress, CreateIntent(FindPoiNearAddress, PointOfInterestLuis.Intent.FindPointOfInterest, address: new string[] { ContextStrings.Ave }));
        }

        public static string WhatsNearby { get; } = "What's nearby?";

        public static string FindNearestPoi { get; } = "find closest poi nearby me";

        public static string FindPharmacy { get; } = $"find a {ContextStrings.Pharmacy} nearby me";

        public static string FindPoiNearAddress { get; } = $"find poi near {ContextStrings.Ave}";
    }
}
