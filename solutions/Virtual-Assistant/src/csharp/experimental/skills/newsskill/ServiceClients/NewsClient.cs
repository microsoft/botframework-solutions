using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models;

namespace NewsSkill
{
    public class NewsClient
    {
        private NewsSearchClient _client;

        public NewsClient(string key)
        {
            _client = new NewsSearchClient(new ApiKeyServiceClientCredentials(key));
        }

        public async Task<IList<NewsArticle>> GetNewsForTopic(string query)
        {
            var results = await _client.News.SearchAsync(query, market: "en-us", count: 10);
            return results.Value;
        }

        public async Task<IList<NewsTopic>> GetTrendingNews()
        {
            var results = await _client.News.TrendingAsync(market: "en-us", count: 10);
            return results.Value;
        }
    }
}
