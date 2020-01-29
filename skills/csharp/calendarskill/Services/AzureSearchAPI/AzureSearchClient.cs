using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Rest.Azure;

namespace CalendarSkill.Services.AzureSearchAPI
{
    public class AzureSearchClient : ISearchService
    {
        private static ISearchIndexClient _indexClient;

        public AzureSearchClient(string searchServiceName, string searchServiceAdminApiKey, string searchIndexName)
        {
            ISearchServiceClient searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAdminApiKey));
            _indexClient = searchClient.Indexes.GetClient(searchIndexName);
        }

        public async Task<List<RoomModel>> GetMeetingRoomAsync(string query, int floorNumber = 0)
        {
            // Enable fuzzy match.
            query = query == "*" ? query : query + "*";

            List<RoomModel> meetingRooms = new List<RoomModel>();

            DocumentSearchResult<RoomModel> searchResults = await SearchMeetongRoomAsync(SearchMode.All, query, floorNumber);

            if (searchResults.Results.Count() == 0)
            {
                searchResults = await SearchMeetongRoomAsync(SearchMode.Any, query, floorNumber);
            }

            foreach (SearchResult<RoomModel> result in searchResults.Results)
            {
                // Only EmailAddress is required and we will use it to book the room.
                if (string.IsNullOrEmpty(result.Document.EmailAddress))
                {
                    throw new Exception("EmailAddress of meeting room is null");
                }

                if (string.IsNullOrEmpty(result.Document.DisplayName))
                {
                    result.Document.DisplayName = result.Document.EmailAddress;
                }

                meetingRooms.Add(result.Document);
            }

            return meetingRooms;
        }

        private static SkillException HandleAzureSearchException(Exception ex)
        {
            var skillExceptionType = SkillExceptionType.Other;
            if (ex is CloudException)
            {
                var cex = ex as CloudException;
                if (cex.Response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    skillExceptionType = SkillExceptionType.APIForbidden;
                }
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }

        private async Task<DocumentSearchResult<RoomModel>> SearchMeetongRoomAsync(SearchMode searchMode, string query, int floorNumber = 0)
        {
            SearchParameters parameters = new SearchParameters()
            {
                SearchMode = searchMode,
                Filter = floorNumber == 0 ? null : SearchFilters.FloorNumberFilter + floorNumber.ToString()
            };
            try
            {
                DocumentSearchResult<RoomModel> searchResults = await _indexClient.Documents.SearchAsync<RoomModel>(query, parameters);
                return searchResults;
            }
            catch (Exception ex)
            {
                throw HandleAzureSearchException(ex);
            }
        }

        private class SearchFilters
        {
            public const string FloorNumberFilter = "FloorNumber eq ";
        }
    }
}
