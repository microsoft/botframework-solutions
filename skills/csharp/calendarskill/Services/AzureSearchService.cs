using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;

namespace CalendarSkill.Services
{
    public class AzureSearchService
    {
        private static ISearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;
        private readonly string _searchIndexName;
        private readonly string _authKey;
        private readonly Uri _cosmosEndpoint;
        private readonly string _databaseId;
        private readonly string _collectionId;

        public AzureSearchService(BotSettings settings)
        {
            _searchClient = new SearchServiceClient(settings.AzureSearch.SearchServiceName, new SearchCredentials(settings.AzureSearch.SearchServiceAdminApiKey));
            //_indexClient = _searchClient.Indexes.GetClient(settings.AzureSearch.SearchIndexName);
            _searchIndexName = settings.AzureSearch.SearchIndexName;
            _authKey = settings.CosmosDb.AuthKey;
            _cosmosEndpoint = settings.CosmosDb.CosmosDBEndpoint;
            _databaseId = settings.AzureSearch.SourceDbName;
            _collectionId = settings.AzureSearch.SourceCollectionName;
        }

        public async Task<List<PlaceModel>> GetMeetingRoomAsync(string name)
        {
            try
            {
                List<PlaceModel> meetingRooms = new List<PlaceModel>();
                _indexClient = _searchClient.Indexes.GetClient(_searchIndexName);
                SearchParameters parameters = new SearchParameters()
                {
                    SearchMode = SearchMode.All
                };
                DocumentSearchResult<Document> searchResult = await _indexClient.Documents.SearchAsync(name, parameters);

                if (searchResult.Results.Count() == 0)
                {
                    parameters.SearchMode = SearchMode.Any;
                    searchResult = await _indexClient.Documents.SearchAsync(name, parameters);
                }

                foreach (var item in searchResult.Results)
                {
                    meetingRooms.Add(new PlaceModel()
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

        public async Task<List<PlaceModel>> GetMeetingRoomByBuildingAsync(string building)
        {
            try
            {
                List<PlaceModel> meetingRooms = new List<PlaceModel>();
                _indexClient = _searchClient.Indexes.GetClient(_searchIndexName);
                SearchParameters parameters = new SearchParameters()
                {
                    SearchMode = SearchMode.All
                };
                DocumentSearchResult<Document> searchResult = await _indexClient.Documents.SearchAsync(building, parameters);

                if (searchResult.Results.Count() == 0)
                {
                    parameters.SearchMode = SearchMode.Any;
                    searchResult = await _indexClient.Documents.SearchAsync(building, parameters);
                }

                foreach (var item in searchResult.Results)
                {
                    meetingRooms.Add(new PlaceModel()
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

        public async Task<List<PlaceModel>> GetMeetingRoomByBuildingAndFloorNumberAsync(string building, int floorNumber)
        {
            try
            {
                List<PlaceModel> meetingRooms = new List<PlaceModel>();
                _indexClient = _searchClient.Indexes.GetClient(_searchIndexName);
                SearchParameters parameters = new SearchParameters()
                {
                    //SearchFields = new[] { "Building" },
                    Filter = "FloorNumber eq " + floorNumber.ToString(),
                    SearchMode = SearchMode.All,
                };
                DocumentSearchResult<Document> searchResult = await _indexClient.Documents.SearchAsync(building, parameters);

                if (searchResult.Results.Count() == 0)
                {
                    parameters.SearchMode = SearchMode.Any;
                    searchResult = await _indexClient.Documents.SearchAsync(building, parameters);
                }

                foreach (var item in searchResult.Results)
                {
                    meetingRooms.Add(new PlaceModel()
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

        public async Task<bool> CheckDataValid(string name)
        {
            return true;
        }

        public async Task BuildIndexAsync()
        {
            if (_searchClient.Indexes.Exists(_searchIndexName))
            {
                _searchClient.Indexes.Delete(_searchIndexName);
            }

            var defination = new Index()
            {
                Name = _searchIndexName,
                Fields = FieldBuilder.BuildForType<MeetingRoom>()
            };

            var res1 = await _searchClient.Indexes.CreateAsync(defination);

            string cosmosDBConnectionString = "AccountEndpoint=" + _cosmosEndpoint + ";AccountKey=" + _authKey + ";Database=" + _databaseId;

            DataSource dataSource = DataSource.CosmosDb(
                name: "new-datasource",
                cosmosDbConnectionString: cosmosDBConnectionString,
                collectionName: _collectionId,
                useChangeDetection: true);

            var res2 = await _searchClient.DataSources.CreateOrUpdateAsync(dataSource);

            Indexer indexer = new Indexer(
                name: "new-indexer",
                dataSourceName: dataSource.Name,
                targetIndexName: _searchIndexName);

            if (_searchClient.Indexers.Exists(indexer.Name))
            {
                _searchClient.Indexers.Reset(indexer.Name);
            }

            var res3 = await _searchClient.Indexers.CreateOrUpdateAsync(indexer);

            await _searchClient.Indexers.RunAsync(indexer.Name);
        }

        public partial class MeetingRoom
        {
            [System.ComponentModel.DataAnnotations.Key]
            [IsSearchable]
            public string Id { get; set; }

            [IsSearchable]
            public string DisplayName { get; set; }

            [IsFilterable]
            public string EmailAddress { get; set; }

            [IsFilterable]
            public int Capacity { get; set; }

            [IsSearchable]
            public string Building { get; set; }

            [IsFilterable]
            public int FloorNumber { get; set; }
        }
    }
}
