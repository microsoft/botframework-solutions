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
    public class AzureSearchClient
    {
        private static ISearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;

        public AzureSearchClient(BotSettings settings)
        {
            _searchClient = new SearchServiceClient(settings.AzureSearch.SearchServiceName, new SearchCredentials(settings.AzureSearch.SearchServiceAdminApiKey));
            _indexClient = _searchClient.Indexes.GetClient(settings.AzureSearch.SearchIndexName);
        }

        public async Task<List<PlaceModel>> GetMeetingRoomByTitleAsync(string name)
        {
            try
            {
                List<PlaceModel> meetingRooms = new List<PlaceModel>();

                DocumentSearchResult<Document> searchResult = await _indexClient.Documents.SearchAsync(name);
                foreach (var item in searchResult.Results)
                {
                    meetingRooms.Add(new PlaceModel()
                    {
                        Id = item.Document["id"].ToString(),
                        DisplayName = item.Document["displayName"].ToString(),
                        EmailAddress = item.Document["emailAddress"].ToString(),
                        Capacity = int.Parse(item.Document["capacity"].ToString())
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
