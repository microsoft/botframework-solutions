using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using PointOfInterestSkill.Models;

namespace PointOfInterestSkill
{
    public class PointOfInterestSkillState
    {
        public PointOfInterestSkillState()
        {
            CurrentCoordinates = null;
            Destination = null;
            APIKey = string.Empty;
            DialogName = string.Empty;
            Keyword = string.Empty;
            Address = string.Empty;
            CommonLocation = string.Empty;
            CurrentCoordinates = null;
            LastFoundPointOfInterests = null;
            ActiveRoute = null;
            RouteType = string.Empty;
            UserSelectIndex = -1;
        }

        public UserInformation UserInfo { get; set; }

        public LatLng CurrentCoordinates { get; set; }

        public PointOfInterestModel Destination { get; set; }

        public LatLng Home { get; set; }

        public LatLng Office { get; set; }

        public List<PointOfInterestModel> LastFoundPointOfInterests { get; set; }

        public RouteDirections.Route ActiveRoute { get; set; }

        public List<RouteDirections.Route> FoundRoutes { get; set; }

        public string APIKey { get; set; }

        public string DialogName { get; set; }

        /// <summary>
        /// Gets or sets LUIS key entity.
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// Gets or sets LUIS address entity.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets LUIS phrase list entity used to match: here, destination, home, office.
        /// </summary>
        public string CommonLocation { get; set; }

        public string RouteType { get; set; }

        public PointOfInterest LuisResult { get; set; }

        public DialogState ConversationDialogState { get; set; }

        public int UserSelectIndex { get; set; }

        public void Clear()
        {
            Destination = null;
            APIKey = null;
            DialogName = string.Empty;
            Keyword = string.Empty;
            Address = string.Empty;
            RouteType = string.Empty;
            LastFoundPointOfInterests = null;
            UserSelectIndex = -1;
            CommonLocation = string.Empty;
        }

        /// <summary>
        /// Clear LUIS results in state for next dialog turn.
        /// </summary>
        public void ClearLuisResults()
        {
            Keyword = string.Empty;
            Address = string.Empty;
            RouteType = string.Empty;
            UserSelectIndex = -1;
        }

        /// <summary>
        /// Gets the origin coordinates for API calls based on what COMMON_LOCATION entity has matched.
        /// </summary>
        public LatLng GetCommonLocationCoordinates(string commonLocation)
        {
            LatLng commonLocationCoordinates = null;

            if (!string.IsNullOrEmpty(commonLocation))
            {
                switch (CommonLocation)
                {
                    case "home":
                        if (Home != null)
                        {
                            commonLocationCoordinates = Home;
                        }
                        else
                        {
                            throw new Exception("The bot state is missing any home coordinates. Make sure your event architecture is correctly configured to send the \"IPA.Home\" event.");
                        }

                        break;
                    case "office":
                        if (Office != null)
                        {
                            commonLocationCoordinates = Office;
                        }
                        else
                        {
                            throw new Exception("The bot state is missing any office coordinates. Make sure your event architecture is correctly configured to send the \"IPA.Office\" event.");
                        }

                        break;
                    case "destination":
                        if (Destination != null)
                        {
                            commonLocationCoordinates = new LatLng() { Latitude = Destination.Geolocation.Latitude, Longitude = Destination.Geolocation.Longitude };
                        }

                        break;
                    case "here":
                    default:
                        if (CurrentCoordinates != null)
                        {
                            commonLocationCoordinates = CurrentCoordinates;
                        }
                        else
                        {
                            throw new Exception("The bot state is missing any current coordinates. Make sure your event architecture is correctly configured to send the \"IPA.Location\" event.");
                        }

                        break;
                }
            }
            else
            {
                if (CurrentCoordinates != null)
                {
                    commonLocationCoordinates = CurrentCoordinates;
                }
                else
                {
                    throw new Exception("The bot state is missing any current coordinates. Make sure your event architecture is correctly configured.");
                }
            }

            return commonLocationCoordinates;
        }

        public LatLng GetDestinationCoordinates()
        {
            LatLng destinationCoordinates = null;

            if (Destination != null)
            {
                destinationCoordinates = new LatLng() { Latitude = Destination.Geolocation.Latitude, Longitude = Destination.Geolocation.Longitude };
            }
            else
            {
                throw new Exception("The bot state is missing any destination coordinates. Make sure your event architecture is correctly configured to send the \"IPA.Destination\" event.");
            }

            return destinationCoordinates;
        }
    }
}