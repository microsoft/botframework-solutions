// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Cards;
using PointOfInterestSkill.Models.Foursquare;
using System;

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
        /// Initializes a new instance of the <see cref="PointofInterestModel"/> class from Google event.
        /// </summary>
        /// <param name="azureMapsPoi">Azure Maps point of interest.</param>
        public PointofInterestModel(Location azureMapsPoi)
        {
            source = PointofInterestSource.AzureMaps;
            azureMapsPoiData = azureMapsPoi;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointofInterestModel"/> class from Google event.
        /// </summary>
        /// <param name="foursquarePoi">Foursquare point of interest.</param>
        public PointofInterestModel(Venue foursquarePoi)
        {
            source = PointofInterestSource.Foursquare;
            foursquarePoiData = foursquarePoi;
        }

        public string Id {
            get
            {
                switch (source)
                {
                    case PointofInterestSource.AzureMaps:
                        return azureMapsPoiData.;
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
                    case PointofInterestSource.Microsoft:
                        msftEventData.Id = value;
                        break;
                    case PointofInterestSource.Google:
                        gmailEventData.Id = value;
                        break;
                    default:
                        throw new Exception("Point of Interest type not defined");
                }
            }
        }

        public string ImageUrl { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string SpeakAddress { get; set; }

        public string Geolocation { get; set; }

        public string ETA { get; set; }

        public string Distance { get; set; }

        public string Rating { get; set; }

        public string Price { get; set; }

        public string Hours { get; set; }

        public string Category { get; set; }

        public string Provider { get; set; }

        public int OptionNumber { get; set; }
    }
}