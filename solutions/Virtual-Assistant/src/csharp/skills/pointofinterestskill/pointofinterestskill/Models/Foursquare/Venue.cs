using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Venue
    {

        /// <summary>
        /// Gets or sets the best available photo of this venue.
        /// </summary>
        [JsonProperty(PropertyName = "bestPhoto")]
        public Photo BestPhoto { get; set; }

        /// <summary>
        /// Gets or sets the canonical URL for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "canonicalUrl")]
        public string CanonicalUrl { get; set; }

        /// <summary>
        /// Gets or sets matching categories for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "categories")]
        public Category[] Categories { get; set; }

        /// <summary>
        /// Gets or sets available contact information for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "contact")]
        public Contact Contact { get; set; }

        /// <summary>
        /// Gets or sets the description of this venue.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the available hours for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "hours")]
        public Hours Hours { get; set; }

        /// <summary>
        /// Gets or sets the Foursquare Id for this venue.
        /// </summary>
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
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the price for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public Price Price { get; set; }

        /// <summary>
        /// Gets or sets the rating for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "rating")]
        public float Rating { get; set; }

        /// <summary>
        /// Gets or sets the url for this venue.
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the venue's page.
        /// </summary>
        [JsonProperty(PropertyName = "venuePage")]
        public VenuePage VenuePage { get; set; }
    }
}
