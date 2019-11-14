﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Bot.Builder.Solutions.Responses;
using PointOfInterestSkill.Models.Foursquare;
using PointOfInterestSkill.Responses.Shared;

namespace PointOfInterestSkill.Models
{
    /// <summary>
    /// Point of Interest mapping entity.
    /// </summary>
    public partial class PointOfInterestModel : ICardData
    {
        public const string AzureMaps = "Azure Maps";
        public const string Foursquare = "Foursquare";

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
            Address = !string.IsNullOrEmpty(azureMapsPoi.Address?.FreeformAddress)
                ? azureMapsPoi.Address?.FreeformAddress
                : Address;

            // Set if to be the same as Address for now
            // Change it to proper handling when using AzureMaps again
            AddressForSpeak = Address;
            Geolocation = azureMapsPoi.Position
                ?? Geolocation;
            Category = (azureMapsPoi.Poi?.Classifications != null)
            ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(azureMapsPoi.Poi.Classifications.FirstOrDefault().Names.FirstOrDefault().NameProperty)
            : Category;
            Phone = azureMapsPoi.Poi?.Phone;
            Provider = new SortedSet<string> { AzureMaps };

            // TODO for better display. English style now.
            if (Name == null && Address != null)
            {
                AddressAlternative = new string[] { azureMapsPoi.Address.StreetName, azureMapsPoi.Address.MunicipalitySubdivision, azureMapsPoi.Address.CountrySecondarySubdivision, azureMapsPoi.Address.CountrySubdivisionName, azureMapsPoi.Address.CountryCodeISO3 }.Aggregate((source, acc) => string.IsNullOrEmpty(source) ? acc : (string.IsNullOrEmpty(acc) ? source : $"{source}, {acc}"));
            }

            if (Category == null)
            {
                Category = azureMapsPoi.ResultType;
            }
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
            Provider = new SortedSet<string> { Foursquare };
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
        public string PointOfInterestImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The name of this point of interest.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the formatted address of the point of interest.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The formatted address of this point of interest.
        /// </value>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets an alternative address when use address as name.
        /// </summary>
        /// <value>
        /// The alternative address.
        /// </value>
        public string AddressAlternative { get; set; }

        /// <summary>
        /// Gets or sets the formatted address of the point of interest
        /// that we put in the Speak property of the Activity to support
        /// speech scenarios
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The formatted address of this point of interest for speech.
        /// </value>
        public string AddressForSpeak { get; set; }

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
        /// Gets or sets the name of the provider.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The provider of this provider.
        /// </value>
        public SortedSet<string> Provider { get; set; }

        /// <summary>
        /// Gets or sets the formatted name of providers.
        /// Availability: Azure Maps, Foursquare.
        /// </summary>
        /// <value>
        /// The provider of this point of interest.
        /// </value>
        public string ProviderDisplayText { get; set; }

        /// <summary>
        /// Gets or sets the index number of the point of interest.
        /// </summary>
        /// <value>
        /// The index of this point of interest.
        /// </value>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the formatted speak response of the point of interest.
        /// </summary>
        /// <value>
        /// The formatted speak string.
        /// </value>
        public string Speak { get; set; }

        /// <summary>
        /// Gets or sets the raw string for speak when it is decorated. Could be used as choice value.
        /// </summary>
        /// <value>
        /// The raw speak string.
        /// </value>
        public string RawSpeak { get; set; }

        /// <summary>
        /// Gets or sets phone.
        /// </summary>
        /// <value>
        /// Phone.
        /// </value>
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets the text to submit.
        /// </summary>
        /// <value>
        /// The text to submit.
        /// </value>
        public string SubmitText { get; set; }

        /// <summary>
        /// Gets or sets the card title.
        /// </summary>
        /// <value>
        /// The text to submit.
        /// </value>
        public string CardTitle { get; set; }

        public string ActionCall { get; set; }

        public string ActionShowDirections { get; set; }

        public string ActionStartNavigation { get; set; }

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

        public string GenerateProviderDisplayText()
        {
            return string.Format($"{PointOfInterestSharedStrings.POWERED_BY} **{{0}}**", Provider.Aggregate((j, k) => j + " & " + k));
        }
    }
}