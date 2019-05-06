using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BingSearchSkill.Models
{
    public class MovieModel
    {
        public string context { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string name { get; set; }
        public string image { get; set; }
        public string[] genre { get; set; }
        public string contentRating { get; set; }
        public Actor[] actor { get; set; }
        public Director director { get; set; }
        public Creator[] creator { get; set; }
        public string description { get; set; }
        public string datePublished { get; set; }
        public string keywords { get; set; }
        public Aggregaterating aggregateRating { get; set; }
        public Review review { get; set; }
        public string duration { get; set; }
        public Trailer trailer { get; set; }
    }

    public class Director
    {
        public string type { get; set; }
        public string url { get; set; }
        public string name { get; set; }
    }

    public class Aggregaterating
    {
        public string type { get; set; }
        public int ratingCount { get; set; }
        public string bestRating { get; set; }
        public string worstRating { get; set; }
        public string ratingValue { get; set; }
    }

    public class Review
    {
        public string type { get; set; }
        public Itemreviewed itemReviewed { get; set; }
        public Author author { get; set; }
        public string dateCreated { get; set; }
        public string inLanguage { get; set; }
        public string name { get; set; }
        public string reviewBody { get; set; }
        public Reviewrating reviewRating { get; set; }
    }

    public class Itemreviewed
    {
        public string type { get; set; }
        public string url { get; set; }
    }

    public class Author
    {
        public string type { get; set; }
        public string name { get; set; }
    }

    public class Reviewrating
    {
        public string type { get; set; }
        public string worstRating { get; set; }
        public string bestRating { get; set; }
        public string ratingValue { get; set; }
    }

    public class Trailer
    {
        public string type { get; set; }
        public string name { get; set; }
        public string embedUrl { get; set; }
        public Thumbnail thumbnail { get; set; }
        public string thumbnailUrl { get; set; }
        public string description { get; set; }
        public DateTime uploadDate { get; set; }
    }

    public class Thumbnail
    {
        public string type { get; set; }
        public string contentUrl { get; set; }
    }

    public class Actor
    {
        public string type { get; set; }
        public string url { get; set; }
        public string name { get; set; }
    }

    public class Creator
    {
        public string type { get; set; }
        public string url { get; set; }
        public string name { get; set; }
    }

}
