using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BingSearchSkill.Models;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BingSearchSkill.Services
{
    public class BingSearchClient
    {
        private EntitySearchClient _entitySearchClient;
        private WebSearchClient _webSearchClient;
        private string _bingAnswerSearchKey;

        public BingSearchClient(string bingSearchKey, string bingAnswerSearchKey)
        {
            _entitySearchClient = new EntitySearchClient(new Microsoft.Azure.CognitiveServices.Search.EntitySearch.ApiKeyServiceClientCredentials(bingSearchKey));
            _webSearchClient = new WebSearchClient(new Microsoft.Azure.CognitiveServices.Search.WebSearch.ApiKeyServiceClientCredentials(bingSearchKey));
            _bingAnswerSearchKey = bingAnswerSearchKey;
        }

        private async Task<Entities> GetEntitySearchResult(string query)
        {
            try
            {
                var searchResponse = await _entitySearchClient.Entities.SearchAsync(query);
                return searchResponse.Entities;
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        private async Task<Microsoft.Azure.CognitiveServices.Search.WebSearch.Models.SearchResponse> GetWebSearchResult(string query)
        {
            try
            {
                var searchResponse = await _webSearchClient.Web.SearchAsync(query);
                return searchResponse;
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        public async Task<List<SearchResultModel>> GetSearchResult(string query, SearchResultModel.EntityType queryType = SearchResultModel.EntityType.Unknown)
        {
            var results = new List<SearchResultModel>();
            var answerSearchResult = await GetAnswerSearchResult(query);
            if (answerSearchResult != null)
            {
                results.Add(answerSearchResult);
                return results;
            }

            var entitySearchResult = await GetEntitySearchResult(query);
            var webSearchResult = await GetWebSearchResult(query);
            if (entitySearchResult != null)
            {
                foreach (var entity in entitySearchResult.Value)
                {
                    results.Add(new SearchResultModel(entity));
                }
            }

            if (webSearchResult != null && webSearchResult.WebPages != null)
            {
                foreach (var webResult in webSearchResult.WebPages.Value)
                {
                    results.Add(new SearchResultModel(webResult.Url));
                }
            }

            var personResults = new List<SearchResultModel>();
            var movieResults = new List<SearchResultModel>();
            foreach (var result in results)
            {
                if (result.Type == SearchResultModel.EntityType.Person)
                {
                    personResults.Add(result);
                }
                else if (result.Type == SearchResultModel.EntityType.Movie)
                {
                    movieResults.Add(result);
                }
            }

            switch (queryType)
            {
                case SearchResultModel.EntityType.Person:
                    return personResults;
                case SearchResultModel.EntityType.Movie:
                    return movieResults;
                default:
                    if (personResults.Any())
                    {
                        return personResults;
                    }
                    else
                    {
                        return movieResults;
                    }
            }
        }

        private async Task<SearchResultModel> GetAnswerSearchResult(string query)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _bingAnswerSearchKey);
            var responseString = await httpClient.GetStringAsync($"https://api.labs.cognitive.microsoft.com/answerSearch/v7.0/search?q={query}&mkt=en-us");
            var responseJson = JToken.Parse(responseString);
            var factsJson = responseJson["facts"];
            if (factsJson != null)
            {
                var fact = factsJson.ToObject<FactModel>();
                return new SearchResultModel(fact);
            }

            return null;
        }
    }
}
