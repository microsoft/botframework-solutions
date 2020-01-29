// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Bot.Solutions.Responses;
using SkillServiceLibrary.Responses;

namespace SkillServiceLibrary.Models
{
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