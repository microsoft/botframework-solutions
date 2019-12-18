using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;

namespace CalendarSkill.Services.AzureSearchAPI
{
    public class AzureSearchClient : ISearchService
    {
        private static ISearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;
        private readonly string _searchIndexName;

        public AzureSearchClient(string searchServiceName, string searchServiceAdminApiKey, string searchIndexName)
        {
            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAdminApiKey));
            _searchIndexName = searchIndexName;
        }

        public async Task<List<RoomModel>> GetMeetingRoomAsync(string query, int floorNumber = 0)
        {
            try
            {
                List<RoomModel> meetingRooms = new List<RoomModel>();
                _indexClient = _searchClient.Indexes.GetClient(_searchIndexName);
                SearchParameters parameters = new SearchParameters()
                {
                    SearchMode = SearchMode.All,
                    Filter = floorNumber == 0 ? null : "FloorNumber eq " + floorNumber.ToString()
                };

                DocumentSearchResult<Document> searchResult = await _indexClient.Documents.SearchAsync(query, parameters);

                if (searchResult.Results.Count() == 0)
                {
                    parameters.SearchMode = SearchMode.Any;
                    searchResult = await _indexClient.Documents.SearchAsync(query, parameters);
                }

                foreach (var item in searchResult.Results)
                {
                    meetingRooms.Add(new RoomModel()
                    {
                        Id = item.Document["Id"].ToString(),
                        DisplayName = item.Document["DisplayName"].ToString(),
                        EmailAddress = item.Document["EmailAddress"].ToString(),
                        Building = item.Document["Building"].ToString(),
                        FloorNumber = int.Parse(item.Document["FloorNumber"].ToString())
                    });
                }

                return meetingRooms;
            }
            catch
            {
                return null;
            }
        }
    }
}
