﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BingSearchSkill.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            var movieInfo = JToken.Parse(movieInfoJsonString);
            return new MovieModel(movieInfo);
        }
    }
}
