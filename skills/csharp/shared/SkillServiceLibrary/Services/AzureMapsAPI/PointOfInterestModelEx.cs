// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkillServiceLibrary.Models.AzureMaps;
using SkillServiceLibrary.Services.AzureMapsAPI;

namespace SkillServiceLibrary.Models
{
    public partial class PointOfInterestModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class from Azure Maps Point of Interest.
        /// </summary>
        /// <param name="azureMapsPoi">Azure Maps point of interest.</param>
        /// <param name="provider">Azure Maps provider.</param>
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
            Provider = new SortedSet<string> { AzureMapsGeoSpatialService.ProviderName };

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
    }
}
