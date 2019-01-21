// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
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
    public partial class PointofInterestModel : CardDataBase
    {
        /// <summary>
        /// The point of interest source.
        /// </summary>
        private PointofInterestSource source;

        /// <summary>
        /// The point of interest data of Azure Maps.
        /// </summary>
        private Location azureMapsPoiData;

        /// <summary>
        /// The point of interst data of Foursquare.
        /// </summary>
        private Venue foursquarePoiData;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointofInterestModel"/> class.、
        /// DO NOT USE THIS ONE.
        /// </summary>
        public PointofInterestModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointofInterestModel"/> class.
        /// </summary>
        /// <param name="source">the event source.</param>
        public PointofInterestModel(PointofInterestSource source)
        {
            this.source = source;
            switch (this.source)
            {
                case PointofInterestSource.AzureMaps:
                    azureMapsPoiData = new Location();
                    break;
                case PointofInterestSource.Foursquare:
                    foursquarePoiData = new Venue();
                    break;
                default:
                    throw new Exception("Point of Interest type not defined");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointofInterestModel"/> class from Azure Maps Point of Interest.
        /// </summary>
        /// <param name="azureMapsPoi">Azure Maps point of interest.</param>
        public PointofInterestModel(Location azureMapsPoi)
        {
            source = PointofInterestSource.AzureMaps;
            azureMapsPoiData = azureMapsPoi;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointofInterestModel"/> class from Foursquare Point of Interest.
        /// </summary>
        /// <param name="foursquarePoi">Foursquare point of interest.</param>
        public PointofInterestModel(Venue foursquarePoi)
        {
            source = PointofInterestSource.Foursquare;
            foursquarePoiData = foursquarePoi;
        }

        public string Id
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return string.Empty;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Id;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        Id = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Id = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string ThumbnailImageUrl
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return string.Empty;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.BestPhoto.AbsoluteUrl;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        ThumbnailImageUrl = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.BestPhoto.AbsoluteUrl = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string Name
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return azureMapsPoiData.Name;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Name;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        azureMapsPoiData.Name = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Name = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string Address
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return azureMapsPoiData.Address.FormattedAddress;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Location.FullFormattedAddress;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        azureMapsPoiData.Name = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Location.FullFormattedAddress = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string AddressLine
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return azureMapsPoiData.Address.AddressLine;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Location.Address;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        azureMapsPoiData.Address.AddressLine = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Location.Address = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public LatLng Geolocation
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return new LatLng() { Latitude = azureMapsPoiData.Point.Coordinates[0], Longitude = azureMapsPoiData.Point.Coordinates[1] };
                    case PointofInterestSource.Foursquare:
                        return new LatLng() { Latitude = foursquarePoiData.Location.Lat, Longitude = foursquarePoiData.Location.Lng};
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        Geolocation = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        Geolocation = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string ETA { get; set; }

        public string Distance { get; set; }

        public string Rating { get; set; }

        public int Price
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return 0;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Price.Tier;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        Price = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Price.Tier = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string Hours
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return string.Empty;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Hours.Status;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        Hours = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Hours.Status = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string Category
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return string.Empty;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Categories.ToString();
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        Hours = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Hours.Status = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string Provider
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return Enum.GetName(typeof(PointofInterestSource), 1);
                    case PointofInterestSource.Foursquare:
                        return Enum.GetName(typeof(PointofInterestSource), 2);
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        Provider = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        Provider = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public int OptionNumber { get; set; }
    }
}