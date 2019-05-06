using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BingSearchSkill.Models
{
    public class MovieModel
    {
        public MovieModel(JToken token)
        {
            Name = token["name"].ToString();
            Description = token["description"].ToString();
            Image = token["image"].ToString();
            Url = token["url"].ToString();
            TrailerUrl = token["trailer"]["embedUrl"].ToString();
            Rating = token["aggregateRating"]["ratingValue"].ToString();
            var genre = token["genre"];
            if (genre is JObject)
            {
                Genre = new List<string>();
                Genre.Add(genre.ToString());
            }
            else
            {
                Genre = genre.ToObject<List<string>>();
            }
        }

        public string Url { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string Rating { get; set; }

        public List<string> Genre { get; set; }

        public string TrailerUrl { get; set; }

        public string Description { get; set; }

    }
}
