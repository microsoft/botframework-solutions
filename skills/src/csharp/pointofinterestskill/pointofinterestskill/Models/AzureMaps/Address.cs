// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    /// <summary>
    /// An address can contain the following fields: address line, locality,
    /// neighborhood, admin district, admin district 2, formatted address, postal code and country or region.
    /// </summary>
    public class Address
    {
        /// <summary>
        /// Gets or sets the official street line of an address relative to the area, as specified by the Locality, or PostalCode, properties.
        /// Typical use of this element would be to provide a street address or any official address.
        /// </summary>
        /// <value>
        /// The official street line of an address relative to the area, as specified by the Locality, or PostalCode, properties.
        /// Typical use of this element would be to provide a street address or any official address.
        /// </value>
        [JsonProperty(PropertyName = "addressLine")]
        public string AddressLine { get; set; }

        /// <summary>
        /// Gets or sets a string specifying the subdivision name in the country or region for an address.
        /// This element is typically treated as the first order administrative subdivision,
        /// but in some cases it is the second, third, or fourth order subdivision in a country, dependency, or region.
        /// </summary>
        /// <value>
        /// A string specifying the subdivision name in the country or region for an address.
        /// This element is typically treated as the first order administrative subdivision,
        /// but in some cases it is the second, third, or fourth order subdivision in a country, dependency, or region.
        /// </value>
        [JsonProperty(PropertyName = "adminDistrict")]
        public string AdminDistrict { get; set; }

        /// <summary>
        /// Gets or sets a string specifying the subdivision name in the country or region for an address.
        /// This element is used when there is another level of subdivision information for a location, such as the county.
        /// </summary>
        /// <value>
        /// A string specifying the subdivision name in the country or region for an address.
        /// This element is used when there is another level of subdivision information for a location, such as the county.
        /// </value>
        [JsonProperty(PropertyName = "adminDistrict2")]
        public string AdminDistrict2 { get; set; }

        /// <summary>
        /// Gets or sets a string specifying the country or region name of an address.
        /// </summary>
        /// <value>
        /// A string specifying the country or region name of an address.
        /// </value>
        [JsonProperty(PropertyName = "countryRegion")]
        public string CountryRegion { get; set; }

        /// <summary>
        /// Gets or sets a string specifying the complete address. This address may not include the country or region.
        /// </summary>
        /// <value>
        /// A string specifying the complete address. This address may not include the country or region.
        /// </value>
        [JsonProperty(PropertyName = "formattedAddress")]
        public string FormattedAddress { get; set; }

        /// <summary>
        /// Gets or sets a string specifying the populated place for the address.
        /// This typically refers to a city, but may refer to a suburb or a neighborhood in certain countries.
        /// </summary>
        /// <value>
        /// A string specifying the populated place for the address.
        /// This typically refers to a city, but may refer to a suburb or a neighborhood in certain countries.
        /// </value>
        [JsonProperty(PropertyName = "locality")]
        public string Locality { get; set; }

        /// <summary>
        /// Gets or sets a string specifying the post code, postal code, or ZIP Code of an address.
        /// </summary>
        /// <value>
        /// A string specifying the post code, postal code, or ZIP Code of an address.
        /// </value>
        [JsonProperty(PropertyName = "postalCode")]
        public string PostalCode { get; set; }
    }
}