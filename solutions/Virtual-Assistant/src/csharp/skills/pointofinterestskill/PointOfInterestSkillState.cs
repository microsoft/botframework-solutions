using Luis;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            CurrentCoordinates = new LatLng() { Latitude = 47.640568390488625, Longitude = -122.1293731033802 };
            FoundLocations = null;
            ActiveRoute = null;
            SearchDescriptor = string.Empty;
            LastUtteredNumber = null;
        }

        public UserInformation UserInfo { get; set; }

        public LatLng CurrentCoordinates { get; set; }

        public Location ActiveLocation { get; set; }

        public List<Location> FoundLocations { get; set; }

        public RouteDirections.Route ActiveRoute { get; set; }

        public List<RouteDirections.Route> FoundRoutes { get; set; }

        public string APIKey { get; set; }

        public string DialogName { get; set; }

        public string SearchText { get; set; }

        public string SearchAddress { get; set; }

        public string SearchDescriptor { get; set; }

        public PointOfInterest LuisResult { get; set; }

        public DialogState ConversationDialogState { get; set; }

        public double[] LastUtteredNumber { get; set; }

        public void Clear()
        {
            ActiveLocation = null;
            APIKey = null;
            DialogName = string.Empty;
            SearchText = string.Empty;
            SearchAddress = string.Empty;
            SearchDescriptor = string.Empty;
            FoundLocations = null;
            LastUtteredNumber = null;
        }
    }
}
