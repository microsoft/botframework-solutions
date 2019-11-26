// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace RestaurantBookingSkill.Utilities
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.AspNetCore.Http;
    using RestaurantBookingSkill.Services;

    public class UrlResolver : IUrlResolver
    {
        private static readonly ConcurrentDictionary<string, string> DataUriCache = new ConcurrentDictionary<string, string>();

        // TODO set this > 0 (like 75) to convert url links of images to jpeg data uris (e.g. when need transcripts)
        private static readonly long UseDataUriJpegQuality = 0L;

        public UrlResolver(IHttpContextAccessor httpContextAccessor, BotSettings settings)
        {
            var httpContext = httpContextAccessor.HttpContext;
            ServerUrl = httpContext.Request.Scheme + "://" + httpContext.Request.Host.Value;
        }

        public string ServerUrl { get; }

        public string GetImageUrl(string imagePath)
        {
            var url = GetImageByCulture(imagePath);

            if (UseDataUriJpegQuality > 0)
            {
                if (!DataUriCache.ContainsKey(url))
                {
                    using (var httpClient = new HttpClient())
                    using (var image = Image.FromStream(httpClient.GetStreamAsync(url).Result))
                    {
                        MemoryStream ms = new MemoryStream();
                        var encoder = ImageCodecInfo.GetImageDecoders().Where(x => x.FormatID == ImageFormat.Jpeg.Guid).FirstOrDefault();
                        var encoderParameters = new EncoderParameters(1);
                        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, UseDataUriJpegQuality);
                        image.Save(ms, encoder, encoderParameters);
                        DataUriCache[url] = $"data:image/jpeg;base64,{Convert.ToBase64String(ms.ToArray())}";
                    }
                }

                url = DataUriCache[url];
            }

            return url;
        }

        private string GetImageByCulture(string imagePath)
        {
            var currentCulture = CultureInfo.CurrentUICulture.Name.Split("-");
            var neutralCulture = currentCulture[0].ToLower();
            string specificCulture = null;

            if (currentCulture.ElementAtOrDefault(1) != null)
            {
                specificCulture = currentCulture[1];
            }

            return GetImagePath(imagePath, neutralCulture, specificCulture);
        }

        private string GetImagePath(string imagePath, string neutralCulture, string specificCulture)
        {
            return $"{ServerUrl}/assets/en/images/{imagePath}";
        }
    }
}
