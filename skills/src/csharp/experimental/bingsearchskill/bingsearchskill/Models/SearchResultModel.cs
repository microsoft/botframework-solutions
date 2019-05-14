using Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BingSearchSkill.Models
{
    public class SearchResultModel
    {
        public SearchResultModel(string url)
        {
            if (url.StartsWith("https://www.imdb.com"))
            {
                Type = EntityType.Movie;
            }
            else
            {
                Type = EntityType.Unknown;
            }

            Url = url;
        }

        public SearchResultModel(Thing thing)
        {
            if (thing.EntityPresentationInfo.EntityTypeHints[0] == "Person")
            {
                Type = EntityType.Person;
            }
            else
            {
                Type = EntityType.Unknown;
            }

            Description = thing.Description;
            ImageUrl = thing.Image.ThumbnailUrl;
            Url = thing.Url ?? thing.WebSearchUrl;
            Name = thing.Name;
        }

        public SearchResultModel(FactModel fact)
        {
            Type = EntityType.Fact;
            Description = fact.value[0].description;
            Url = fact.contractualRules[0].url;
        }

        public enum EntityType
        {
            Person = 1,
            Movie = 2,
            Fact = 3,
            Unknown = 0
        }

        public EntityType Type { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public string Url { get; set; }

        public string Name { get; set; }
    }
}
