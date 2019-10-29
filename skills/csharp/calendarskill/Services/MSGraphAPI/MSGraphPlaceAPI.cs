// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;

namespace CalendarSkill.Services.MSGraphAPI
{
    public class MSGraphPlaceAPI : IPlaceService
    {
        private const string GraphBaseUrl = "https://graph.microsoft.com/beta/places/";
        private HttpClient httpClient;

        public MSGraphPlaceAPI()
        {
        }

        public MSGraphPlaceAPI(string token, HttpClient client = null)
        {
            if (client == null)
            {
                this.httpClient = ServiceHelper.GetHttpClient(token);
            }
            else
            {
                this.httpClient = client;
            }
        }

        /// <summary>
        /// Get meeting room by title.
        /// </summary>
        /// <param name="name">full or prefix of displayName/emailAddress.</param>
        /// <returns>meeting rooms.</returns>
        public async Task<List<PlaceModel>> GetMeetingRoomAsync()
        {
            try
            {
                var requestUrl = GraphBaseUrl + "microsoft.graph.room";
                return await this.ExecutePlaceGetAsync(requestUrl);
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Get meeting room by title.
        /// </summary>
        /// <param name="name">full or prefix of displayName/emailAddress.</param>
        /// <returns>meeting rooms.</returns>
        public async Task<List<PlaceModel>> GetMeetingRoomByTitleAsync(string name)
        {
            try
            {
                var filterString = $"startswith(displayName, '{name}') or startswith(emailAddress,'{name}')";
                var requestUrl = GraphBaseUrl + "microsoft.graph.room?filter=" + filterString;
                return await this.ExecutePlaceGetAsync(requestUrl);
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        private async Task<List<PlaceModel>> ExecutePlaceGetAsync(string url)
        {
            var meetingRooms = new List<PlaceModel>();
            while (!string.IsNullOrEmpty(url))
            {
                var placeObject = await this.ExecuteGraphFetchAsync(url);
                foreach (var task in placeObject.value)
                {
                    meetingRooms.Add(new PlaceModel()
                    {
                        Id = task["id"],
                        DisplayName = task["displayName"].ToString().Replace("_", " "),
                        EmailAddress = task["emailAddress"],
                        Capacity = task["capacity"],
                        Building = task["building"],
                        FloorNumber = task["floorNumber"]
                    });
                }

                url = placeObject["@odata.nextLink"];
            }

            return meetingRooms;
        }

        private async Task<dynamic> ExecuteGraphFetchAsync(string url)
        {
            var result = await this.httpClient.GetAsync(url);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
            if (result.IsSuccessStatusCode)
            {
                return responseContent;
            }
            else
            {
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }

    }
}