// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using SkillServiceLibrary.Models.Foursquare;
using SkillServiceLibrary.Services.FoursquareAPI;

namespace SkillServiceLibrary.Models
{
    public partial class PointOfInterestModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class from Foursquare Point of Interest.
        /// </summary>
        /// <param name="foursquarePoi">Foursquare point of interest.</param>
        public PointOfInterestModel(Venue foursquarePoi)
        {
            Id = !string.IsNullOrEmpty(foursquarePoi.Id)
                ? foursquarePoi.Id
                : Id;
            PointOfInterestImageUrl = !string.IsNullOrEmpty(foursquarePoi.BestPhoto?.AbsoluteUrl)
               ? $"{foursquarePoi.BestPhoto?.Prefix}440x240{foursquarePoi.BestPhoto?.Suffix}"
               : PointOfInterestImageUrl;
            Name = !string.IsNullOrEmpty(foursquarePoi.Name)
                ? foursquarePoi.Name
                : Name;
            Address = foursquarePoi.Location?.FormattedAddress != null
                ? string.Join(", ", foursquarePoi.Location.FormattedAddress.Take(foursquarePoi.Location.FormattedAddress.Length - 1))
                : Address;
            AddressForSpeak = foursquarePoi.Location?.FormattedAddress != null
                ? foursquarePoi.Location.FormattedAddress[0]
                : Address;
            Geolocation = (foursquarePoi.Location != null)
                ? new LatLng() { Latitude = foursquarePoi.Location.Lat, Longitude = foursquarePoi.Location.Lng }
                : Geolocation;
            Price = (foursquarePoi.Price != null)
                ? foursquarePoi.Price.Tier
                : Price;
            Hours = !string.IsNullOrEmpty(foursquarePoi.Hours?.Status)
                ? foursquarePoi.Hours?.Status
                : Hours;
            Rating = foursquarePoi.Rating.ToString("N1")
                ?? Rating;
            RatingCount = foursquarePoi.RatingSignals != 0
                ? foursquarePoi.RatingSignals
                : RatingCount;
            Category = (foursquarePoi.Categories != null)
                ? foursquarePoi.Categories.First().ShortName
                : Category;
            Phone = foursquarePoi.Contact?.Phone;
            Provider = new SortedSet<string> { FoursquareGeoSpatialService.ProviderName };
        }
    }
}
