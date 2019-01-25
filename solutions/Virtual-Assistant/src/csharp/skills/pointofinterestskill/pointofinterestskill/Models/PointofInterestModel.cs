// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;
using Microsoft.Bot.Solutions.Cards;
using PointOfInterestSkill.Models.Foursquare;

namespace PointOfInterestSkill.Models
{
    /// <summary>
    /// Source of event.
    /// </summary>
    public enum PointofInterestSource
    {
        /// <summary>
        /// Point of Interest from Azure Maps.
        /// </summary>
        AzureMaps = 1,

        /// <summary>
        /// Point of Interest from Foursquare.
        /// </summary>
        Foursquare = 2,

        /// <summary>
        /// Point of Interest from other.
        /// </summary>
        Other = 0,
    }

    /// <summary>
    /// Point of Interest mapping entity.
    /// </summary>
    public partial class PointOfInterestModel : CardDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class.、
        /// DO NOT USE THIS ONE.
        /// </summary>
        public PointOfInterestModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class from Azure Maps Point of Interest.
        /// </summary>
        /// <param name="azureMapsPoi">Azure Maps point of interest.</param>
        public PointOfInterestModel(SearchResult azureMapsPoi)
        {
            Id = !string.IsNullOrEmpty(azureMapsPoi.Id)
                ? azureMapsPoi.Id
                : Id;
            Name = !string.IsNullOrEmpty(azureMapsPoi.Poi?.Name)
                ? azureMapsPoi.Poi?.Name
                : Name;
            City = !string.IsNullOrEmpty(azureMapsPoi.Address?.ToBingAddress()?.AdminDistrict)
                ? azureMapsPoi.Address?.ToBingAddress()?.AdminDistrict
                : City;
            Street = !string.IsNullOrEmpty(azureMapsPoi.Address?.ToBingAddress()?.AddressLine)
                ? azureMapsPoi.Address?.ToBingAddress()?.AddressLine
                : Street;
            Geolocation = azureMapsPoi.Position 
                ?? Geolocation;
            Categories = (azureMapsPoi.Poi?.Categories != null)
            ? azureMapsPoi.Poi.Categories
            : Categories;
            Provider = Enum.GetName(typeof(PointofInterestSource), 1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class from Foursquare Point of Interest.
        /// </summary>
        /// <param name="foursquarePoi">Foursquare point of interest.</param>
        public PointOfInterestModel(Venue foursquarePoi)
        {
            Id = !string.IsNullOrEmpty(foursquarePoi.Id)
                ? foursquarePoi.Id
                : Id;
            ImageUrl = !string.IsNullOrEmpty(foursquarePoi.BestPhoto?.AbsoluteUrl)
               ? foursquarePoi.BestPhoto?.AbsoluteUrl
               : ImageUrl;
            Name = !string.IsNullOrEmpty(foursquarePoi.Name)
                ? foursquarePoi.Name
                : Name;
            City = !string.IsNullOrEmpty(foursquarePoi.Location?.City)
                ? foursquarePoi.Location?.City
                : City;
            Street = !string.IsNullOrEmpty(foursquarePoi.Location?.Address)
                ? foursquarePoi.Location?.Address
                : City;
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
            Categories = (foursquarePoi.Categories != null)
                ? foursquarePoi.Categories.Select(x => x.Name).ToArray()
                : Categories;
            Provider = Enum.GetName(typeof(PointofInterestSource), 2);
        }

        /// <summary>
        /// Gets or sets the id of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail image url of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the address of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the address line of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// Gets or sets the geolocation of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        public LatLng Geolocation { get; set; }

        /// <summary>
        /// Gets or sets the estimated time of arrival to the point of interest.
        /// Availability: Azure Maps.
        /// </summary>
        public string EstimatedTimeOfArrival { get; set; }

        /// <summary>
        /// Gets or sets the distance to the point of interest.
        /// Availability: Azure Maps.
        /// </summary>
        public string Distance { get; set; }

        /// <summary>
        /// Gets or sets the rating of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        public string Rating { get; set; }

        /// <summary>
        /// Gets or sets the price level of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// Gets or sets the hours of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        public string Hours { get; set; }

        public string[] Categories { get; set; }

        /// <summary>
        /// Gets or sets the name of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the option number of the point of interest.
        /// </summary>
        public int OptionNumber { get; set; }
    }
}