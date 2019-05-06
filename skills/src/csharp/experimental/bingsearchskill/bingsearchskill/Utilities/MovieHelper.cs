using BingSearchSkill.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BingSearchSkill.Utilities
{
    public class MovieHelper
    {
        public static MovieModel GetMovieInfoFromUrl(string url)
        {
            var webClient = new WebClient();
            var page = webClient.DownloadData(url);
            var pageString = System.Text.Encoding.UTF8.GetString(page);
            var reg = new Regex("<script type=\"application/ld\\+json\">(?<json>(.)*?(?=</script>))</script>", RegexOptions.Singleline);
            var match = reg.Match(pageString);
            var movieInfoJsonString = match.Groups["json"].Value;
            var movieInfo = JsonConvert.DeserializeObject<MovieModel>(movieInfoJsonString);
            return movieInfo;
        }
    }
}
