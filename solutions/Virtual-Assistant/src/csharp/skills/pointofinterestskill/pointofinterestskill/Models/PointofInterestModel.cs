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
        /// The point of interest data of Foursquare.
        /// </summary>
        private Venue foursquarePoiData;

        private string _id;

        private string _thumbnailImageUrl;

        private string _name;

        private string _address;

        private string _addressLine;

        private LatLng _geolocation;

        private int _price;

        private string[] _categories;

        private string _provider;

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
                        return _id;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        _id = azureMapsPoiData.Id = value;
                        break;

                    case PointofInterestSource.Foursquare:
                        _id = foursquarePoiData.Id = value;
                        break;

                    default:
                        _id = value;
                        break;
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
                        return _thumbnailImageUrl;

                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.BestPhoto?.AbsoluteUrl;

                    default:
                        return _thumbnailImageUrl;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.Foursquare:
                        _thumbnailImageUrl = foursquarePoiData.BestPhoto.AbsoluteUrl = value;
                        break;

                    case PointofInterestSource.AzureMaps:
                    default:
                        _thumbnailImageUrl = value;
                        break;
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
                        return azureMapsPoiData.Poi?.Name;

                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Name;

                    default:
                        return _name;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        _name = azureMapsPoiData.Poi.Name = value;
                        break;

                    case PointofInterestSource.Foursquare:
                        _name = foursquarePoiData.Name = value;
                        break;

                    default:
                        _name = value;
                        break;
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
                        return azureMapsPoiData.Address?.ToBingAddress()?.FormattedAddress;

                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Location?.FullFormattedAddress;

                    default:
                        return _address;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        _address = azureMapsPoiData.Address.ToBingAddress().FormattedAddress = value;
                        break;

                    case PointofInterestSource.Foursquare:
                        _address = foursquarePoiData.Location.FullFormattedAddress = value;
                        break;

                    default:
                        _address = value;
                        break;
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
                        return _addressLine;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        _addressLine = azureMapsPoiData.Address.ToBingAddress().AddressLine = value;
                        break;

                    case PointofInterestSource.Foursquare:
                        _addressLine = foursquarePoiData.Location.Address = value;
                        break;

                    default:
                        _addressLine = value;
                        break;
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
                        return azureMapsPoiData.Position;

                    case PointofInterestSource.Foursquare:
                        return new LatLng() { Latitude = foursquarePoiData.Location.Lat, Longitude = foursquarePoiData.Location.Lng };

                    default:
                        return _geolocation;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        _geolocation = azureMapsPoiData.Position = value;
                        break;

                    case PointofInterestSource.Foursquare:
                        _geolocation = value;
                        break;

                    default:
                        _geolocation = value;
                        break;
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
                        return _price;

                    case PointofInterestSource.Foursquare:
                        return foursquarePoiData.Price.Tier;

                    default:
                        return _price;
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
                        _price = foursquarePoiData.Price.Tier = value;
                        break;

                    default:
                        _price = value;
                        break;
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
                        return _hours;
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
                        _hours = foursquarePoiData.Hours.Status = value;
                        break;

                    default:
                        _hours = value;
                        break;
                }
            }
        }

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
                        return _categories;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        _categories = azureMapsPoiData.Poi.Categories = value;
                        break;

                    case PointofInterestSource.Foursquare:
                        _categories = value;
                        break;

                    default:
                        _categories = value;
                        break;
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
                        return _provider;
                }
            }

            set
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        _provider = value;
                        break;

                    case PointofInterestSource.Foursquare:
                        _provider = value;
                        break;

                    default:
                        _provider = value;
                        break;
                }
            }
        }

        public int OptionNumber { get; set; }
    }
}