// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Venue
    {
        /// <summary>
        /// Gets or sets the best available photo of this venue.
        /// </summary>
        /// <value>
        /// The best photo of this venue.
        /// </value>
        [JsonProperty(PropertyName = "bestPhoto")]
        public Photo BestPhoto { get; set; }

        /// <summary>
        /// Gets or sets the canonical URL for this venue.
        /// </summary>
        /// <value>
        /// The canonical URL of this venue.
        /// </value>
        [JsonProperty(PropertyName = "canonicalUrl")]
        public string CanonicalUrl { get; set; }

        /// <summary>
        /// Gets or sets matching categories for this venue.
        /// </summary>
        /// <value>
        /// The categories of this venue.
        /// </value>
        [JsonProperty(PropertyName = "categories")]
        public Category[] Categories { get; set; }

        /// <summary>
        /// Gets or sets available contact information for this venue.
        /// </summary>
        /// <value>
        /// The contact info of this venue.
        /// </value>
        [JsonProperty(PropertyName = "contact")]
        public Contact Contact { get; set; }

        /// <summary>
        /// Gets or sets the description of this venue.
        /// </summary>
        /// <value>
        /// The description of this venue.
        /// </value>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the available hours for this venue.
        /// </summary>
        /// <value>
        /// The business hours of this venue.
        /// </value>
        [JsonProperty(PropertyName = "hours")]
        public Hours Hours { get; set; }

        /// <summary>
        /// Gets or sets the Foursquare Id for this venue.
        /// </summary>
        /// <value>
        /// The id of this venue.
        /// </value>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the location for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "location")]
        public Location Location { get; set; }

        /// <summary>
        /// Gets or sets the name for this venue.
        /// </summary>
        /// <value>
        /// The name of this venue.
        /// </value>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the price for this venue.
        /// </summary>
        /// <value>
        /// The price data of this venue.
        /// </value>
        [JsonProperty(PropertyName = "price")]
        public Price Price { get; set; }

        /// <summary>
        /// Gets or sets the rating for this venue.
        /// </summary>
        /// <value>
        /// The rating of this venue.
        /// </value>
        [JsonProperty(PropertyName = "rating")]
        public float Rating { get; set; }

        /// <summary>
        /// Gets or sets the rating signal count for this venue.
        /// </summary>
        /// <value>
        /// The rating signals of this venue.
        /// </value>
        [JsonProperty(PropertyName = "ratingSignals")]
        public int RatingSignals { get; set; }

        /// <summary>
        /// Gets or sets the url for this venue.
        /// </summary>
        /// <value>
        /// The URL of this venue.
        /// </value>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the venue's page.
        /// </summary>
        /// <value>
        /// The venue page of this venue.
        /// </value>
        [JsonProperty(PropertyName = "venuePage")]
        public VenuePage VenuePage { get; set; }
    }
}
