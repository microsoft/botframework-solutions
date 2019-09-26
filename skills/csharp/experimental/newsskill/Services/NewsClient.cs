using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models;

namespace NewsSkill.Services
{
    public class NewsClient
    {
        private NewsSearchClient _client;

        public NewsClient(string key)
        {
            _client = new NewsSearchClient(new ApiKeyServiceClientCredentials(key));
        }

        public async Task<IList<NewsArticle>> GetNewsForTopic(string query, string mkt)
        {
            // general search by topic
            var results = await _client.News.SearchAsync(query, countryCode: mkt, count: 10);
            return results.Value;
        }

        public async Task<IList<NewsTopic>> GetTrendingNews(string mkt)
        {
            // get articles trending on social media
            var results = await _client.News.TrendingAsync(countryCode: mkt, count: 10);
            return results.Value;
        }

        // see for valid categories: https://docs.microsoft.com/en-us/rest/api/cognitiveservices-bingsearch/bing-news-api-v7-reference#news-categories-by-market
        public async Task<IList<NewsArticle>> GetNewsByCategory(string topic, string mkt)
        {
            // general search by category
            var results = await _client.News.CategoryAsync(category: topic, market: mkt, count: 10);
            return results.Value;
        }
    }
}
