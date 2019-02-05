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
            ActiveLocation = null;
            APIKey = string.Empty;
            DialogName = string.Empty;
            SearchText = string.Empty;
            SearchAddress = string.Empty;
            CurrentCoordinates = null;
            LastFoundPointOfInterests = null;
            ActiveRoute = null;
            SearchDescriptor = string.Empty;
            UserSelectIndex = -1;
        }

        public UserInformation UserInfo { get; set; }

        public LatLng CurrentCoordinates { get; set; }

        public PointOfInterestModel ActiveLocation { get; set; }

        public List<PointOfInterestModel> LastFoundPointOfInterests { get; set; }

        public RouteDirections.Route ActiveRoute { get; set; }

        public List<RouteDirections.Route> FoundRoutes { get; set; }

        public string APIKey { get; set; }

        public string DialogName { get; set; }

        public string SearchText { get; set; }

        public string SearchAddress { get; set; }

        public string SearchDescriptor { get; set; }

        public PointOfInterestLU LuisResult { get; set; }

        public DialogState ConversationDialogState { get; set; }

        public int UserSelectIndex { get; set; }

        public void Clear()
        {
            ActiveLocation = null;
            APIKey = null;
            DialogName = string.Empty;
            SearchText = string.Empty;
            SearchAddress = string.Empty;
            SearchDescriptor = string.Empty;
            LastFoundPointOfInterests = null;
            UserSelectIndex = -1;
        }

        /// <summary>
        /// Clear LUIS results in state for next dialog turn.
        /// </summary>
        public void ClearLuisResults()
        {
            SearchText = string.Empty;
            SearchAddress = string.Empty;
            SearchDescriptor = string.Empty;
            UserSelectIndex = -1;
        }

        public void CheckForValidCurrentCoordinates()
        {
            if (CurrentCoordinates == null)
            {
                throw new Exception("The bot state is missing any current coordinates. Make sure your event architecture is correctly configured.");
            }
        }
    }
}