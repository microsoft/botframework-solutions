// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Bot.Solutions.Responses;
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
    public partial class PointOfInterestModel : ICardData
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
            City = !string.IsNullOrEmpty(azureMapsPoi.Address?.MunicipalitySubdivision)
                ? azureMapsPoi.Address.MunicipalitySubdivision
                : City;
            Street = !string.IsNullOrEmpty(azureMapsPoi.Address?.ToBingAddress()?.AddressLine)
                ? azureMapsPoi.Address?.ToBingAddress()?.AddressLine
                : Street;
            Geolocation = azureMapsPoi.Position
                ?? Geolocation;
            Category = (azureMapsPoi.Poi?.Categories != null)
            ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(azureMapsPoi.Poi.Categories.First().ToLower())
            : Category;
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
            RatingCount = foursquarePoi.RatingSignals != 0
                ? foursquarePoi.RatingSignals
                : RatingCount;
            Category = (foursquarePoi.Categories != null)
                ? foursquarePoi.Categories.First().ShortName
                : Category;
            Provider = Enum.GetName(typeof(PointofInterestSource), 2);
        }

        /// <summary>
        /// Gets or sets the id of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The id of this point of interest.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail image url of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The image URL of this point of interest.
        /// </value>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The name of this point of interest.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the address of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The city of this point of interest.
        /// </value>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the address line of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The street address of this point of interest.
        /// </value>
        public string Street { get; set; }

        /// <summary>
        /// Gets or sets the geolocation of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The geolocation of this point of interest.
        /// </value>
        public LatLng Geolocation { get; set; }

        /// <summary>
        /// Gets or sets the estimated time of arrival to the point of interest.
        /// Availability: Azure Maps.
        /// </summary>
        /// <value>
        /// The ETA to this point of interest.
        /// </value>
        public string EstimatedTimeOfArrival { get; set; }

        /// <summary>
        /// Gets or sets the distance to the point of interest.
        /// Availability: Azure Maps.
        /// </summary>
        /// <value>
        /// The distance to this point of interest.
        public string Distance { get; set; }

        /// <summary>
        /// Gets or sets the rating of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        /// <value>
        /// The rating of this point of interest.
        /// </value>
        public string Rating { get; set; }

        /// <summary>
        /// Gets or sets the number of ratings of the point of interest.
        /// </summary>
        /// <value>
        /// The rating count of this point of interest.
        /// </value>
        public int RatingCount { get; set; }

        /// <summary>
        /// Gets or sets the price level of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        /// <value>
        /// The price of this point of interest.
        /// </value>
        public int Price { get; set; }

        /// <summary>
        /// Gets or sets the hours of the point of interest.
        /// Availability: Foursquare.
        /// </summary>
        /// <value>
        /// The open hours of this point of interest.
        /// </value>
        public string Hours { get; set; }

        /// <summary>
        /// Gets or sets the top category of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The top category of this point of interest.
        /// </value>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the name of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The provider of this point of interest.
        /// </value>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the index number of the point of interest.
        /// </summary>
        /// <value>
        /// The index of this point of interest.
        /// </value>
        public int Index { get; set; }

        /// <summary>
        /// Gets the formatted string for available details.
        /// </summary>
        /// <value>
        /// The available details formatted string of this point of interest.
        /// </value>
        public string AvailableDetails
        {
            get
            {
                StringBuilder availableDetailsString = new StringBuilder();

                if (!string.IsNullOrEmpty(Category))
                {
                    availableDetailsString.Append(Category);
                }

                if (!string.IsNullOrEmpty(Rating))
                {
                    availableDetailsString.Append($" · {Rating} ⭐");
                }

                if (RatingCount != 0)
                {
                    availableDetailsString.Append($" ({RatingCount})");
                }

                if (Price != 0)
                {
                    availableDetailsString.Append($" · {new string('$', Price)}");
                }

                return availableDetailsString.ToString();
            }
        }
    }
}