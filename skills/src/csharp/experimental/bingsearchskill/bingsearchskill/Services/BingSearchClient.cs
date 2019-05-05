using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingSearchSkill.Models;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;
using Newtonsoft.Json;

namespace BingSearchSkill.Services
{
    public class BingSearchClient
    {
        private EntitySearchClient _entitySearchClient;
        private WebSearchClient _webSearchClient;

        public BingSearchClient(string key)
        {
            _entitySearchClient = new EntitySearchClient(new Microsoft.Azure.CognitiveServices.Search.EntitySearch.ApiKeyServiceClientCredentials(key));
            _webSearchClient = new WebSearchClient(new Microsoft.Azure.CognitiveServices.Search.WebSearch.ApiKeyServiceClientCredentials(key));
        }

        private async Task<Entities> GetEntitySearchResult(string query)
        {
            var searchResponse = await _entitySearchClient.Entities.SearchAsync(query);
            return searchResponse.Entities;
        }

        private async Task<Microsoft.Azure.CognitiveServices.Search.WebSearch.Models.SearchResponse> GetWebSearchResult(string query)
        {
            var searchResponse = await _webSearchClient.Web.SearchAsync(query);
            return searchResponse;
        }

        public async Task<List<SearchResultModel>> GetSearchResult(string query)
        {
            var entitySearchResult = await GetEntitySearchResult(query);
            var webSearchResult = await GetWebSearchResult(query);
            return null;
        }
    }
}
