using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models;
using Newtonsoft.Json;

namespace BingSearchSkill.Services
{
    public class BingSearchClient
    {
        private EntitySearchClient _client;

        public BingSearchClient(string key)
        {
            _client = new EntitySearchClient(new ApiKeyServiceClientCredentials(key));
        }

        public async Task<Entities> GetSearchResult(string query)
        {
            var entityData = await _client.Entities.SearchAsync(query);
            return entityData.Entities;
        }
    }
}
