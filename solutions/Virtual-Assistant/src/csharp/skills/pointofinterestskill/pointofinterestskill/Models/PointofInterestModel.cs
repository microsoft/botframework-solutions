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
        /// The point of interest source.
        /// </summary>
        private PointofInterestSource source;

        /// <summary>
        /// The point of interest data of Azure Maps.
        /// </summary>
        private SearchResult azureMapsPoiData;

        /// <summary>
        /// The point of interst data of Foursquare.
        /// </summary>
        private Venue foursquarePoiData;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class.、
        /// DO NOT USE THIS ONE.
        /// </summary>
        public PointOfInterestModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class.
        /// </summary>
        /// <param name="source">the event source.</param>
        public PointOfInterestModel(PointofInterestSource source)
        {
            this.source = source;
            switch (this.source)
            {
                case PointofInterestSource.AzureMaps:
                    azureMapsPoiData = new SearchResult();
                    break;
                case PointofInterestSource.Foursquare:
                    foursquarePoiData = new Venue();
                    break;
                default:
                    throw new Exception("Point of Interest type not defined");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class from Azure Maps Point of Interest.
        /// </summary>
        /// <param name="azureMapsPoi">Azure Maps point of interest.</param>
        public PointOfInterestModel(SearchResult azureMapsPoi)
        {
            source = PointofInterestSource.AzureMaps;
            azureMapsPoiData = azureMapsPoi;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestModel"/> class from Foursquare Point of Interest.
        /// </summary>
        /// <param name="foursquarePoi">Foursquare point of interest.</param>
        public PointOfInterestModel(Venue foursquarePoi)
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
                        return azureMapsPoiData.Id;
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
                        azureMapsPoiData.Id = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Id = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        private string _thumbnailImageUrl;

        public string ThumbnailImageUrl
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return _thumbnailImageUrl;
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
                        _thumbnailImageUrl = value;
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
                        return (azureMapsPoiData.Poi != null && !string.IsNullOrEmpty(azureMapsPoiData.Poi.Name)) ? azureMapsPoiData.Poi.Name : azureMapsPoiData.Address.FreeformAddress;
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
                        azureMapsPoiData.Poi.Name = value;
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
                        return azureMapsPoiData.Address.ToBingAddress().FormattedAddress;
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
                        azureMapsPoiData.Address.ToBingAddress().FormattedAddress = value;
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
                        return azureMapsPoiData.Address.ToBingAddress().AddressLine;
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
                        azureMapsPoiData.Address.ToBingAddress().AddressLine = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Location.Address = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        private LatLng _geolocation;

        public LatLng Geolocation
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return azureMapsPoiData.Position;
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
                        azureMapsPoiData.Position = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        _geolocation = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string ETA { get; set; }

        public string Distance { get; set; }

        public string Rating { get; set; }

        private int _price;

        public int Price
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return _price;
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
                        _price = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Price.Tier = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }


        private string _hours;

        public string Hours
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return _hours;
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
                        _hours = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        foursquarePoiData.Hours.Status = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }


        private string[] _categories;

        public string[] Categories
        {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return azureMapsPoiData.Poi.Categories;
                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Categories.Select(x => x.Name).ToArray();
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        azureMapsPoiData.Poi.Categories = value;
                        break;
                    case PointofInterestSource.Foursquare:
                        _categories = value;
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