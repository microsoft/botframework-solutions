// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace SkillServiceLibrary.Fakes.AzureMapsAPI.Fakes
{
    public class MockHttpClientHandlerGen
    {
        private readonly HttpClientHandler httpClientHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockHttpClientHandlerGen"/> class.
        /// </summary>
        public MockHttpClientHandlerGen()
        {
            this.httpClientHandler = this.GenerateMockHttpClientHandler();
        }

        public HttpClientHandler GetMockHttpClientHandler()
        {
            return this.httpClientHandler;
        }

        private static bool ShouldReturnMultipleRoutes(HttpRequestMessage httpRequestMessage)
        {
            var places = new string[] { "query=47.63962,-122.13061:47.63967,-122.13029&" };
            var uri = httpRequestMessage.RequestUri.ToString();
            return uri.StartsWith("https://atlas.microsoft.com/route/directions/json") && places.Any(place => uri.Contains(place));
        }

        private HttpClientHandler GenerateMockHttpClientHandler()
        {
            var mockClient = new Mock<HttpClientHandler>(MockBehavior.Strict);
            this.SetHttpMockBehavior(ref mockClient);
            return mockClient.Object;
        }

        private void SetHttpMockBehavior(ref Mock<HttpClientHandler> mockClient)
        {
            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://atlas.microsoft.com/search/nearby/json")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetAzureMapsPointOfInterest()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://atlas.microsoft.com/search/fuzzy/json")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetAzureMapsPointOfInterest()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://atlas.microsoft.com/route/directions/json")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetSingleRouteDirection()),
               });

            // if this fails, fallback to single route
            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => ShouldReturnMultipleRoutes(r)),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetMultipleRouteDirections()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://atlas.microsoft.com/search/address")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetAddressSearch()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://atlas.microsoft.com/search/poi/category/json?api-version=1.0&query=OPEN_PARKING_AREA,PARKING_GARAGE")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetAzureMapsParkingCategory()),
               });

            MemoryStream ms = new MemoryStream();
            using (var image = new Bitmap(1, 1))
            {
                image.SetPixel(0, 0, Color.White);
                image.Save(ms, ImageFormat.Png);
            }

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://atlas.microsoft.com/map/static/png")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new ByteArrayContent(ms.ToArray()),
               });

            // foursquare
            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://api.foursquare.com/v2/venues/")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetFoursquarePointOfInterest()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://api.foursquare.com/v2/venues/explore")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetFoursquarePointOfInterest()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://api.foursquare.com/v2/venues/explore") && r.RequestUri.ToString().Contains("query")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetFoursquareExplore()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://api.foursquare.com/v2/venues/search")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetFoursquarePointOfInterest()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://api.foursquare.com/v2/venues/search?categoryId=4c38df4de52ce0d596b336e1")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetFoursquareParkingCategory()),
               });

        }

        private string GetAzureMapsPointOfInterest()
        {
            return "{\"summary\":{\"queryType\":\"NEARBY\",\"queryTime\":35,\"numResults\":3,\"offset\":0,\"totalResults\":29711,\"fuzzyLevel\":1,\"geoBias\":{\"lat\":47.63962,\"lon\":-122.13061}},\"results\":[{\"type\":\"POI\",\"id\":\"US/POI/p1/101761\",\"score\":-0.011,\"dist\":11.162404707265612,\"info\":\"search:ta:840539001321263-US\",\"poi\":{\"name\":\"Microsoft Way\",\"categories\":[\"bus stop\",\"public transport stop\"],\"classifications\":[{\"code\":\"PUBLIC_TRANSPORT_STOP\",\"names\":[{\"nameLocale\":\"en-US\",\"name\":\"bus stop\"},{\"nameLocale\":\"en-US\",\"name\":\"public transport stop\"}]}]},\"address\":{\"streetName\":\"157th Ave NE\",\"municipality\":\"Redmond\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle East\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98052\",\"extendedPostalCode\":\"980525396\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"157th Ave NE, Redmond, WA 98052\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.63954,\"lon\":-122.1307},\"viewport\":{\"topLeftPoint\":{\"lat\":47.64044,\"lon\":-122.13203},\"btmRightPoint\":{\"lat\":47.63864,\"lon\":-122.12937}},\"entryPoints\":[{\"type\":\"main\",\"position\":{\"lat\":47.63954,\"lon\":-122.1306}}]},{\"type\":\"POI\",\"id\":\"US/POI/p0/5994875\",\"score\":-0.025,\"dist\":24.611388831355395,\"info\":\"search:ta:840539001904749-US\",\"poi\":{\"name\":\"Microsoft Corporation\",\"phone\":\"+(1)-(425)-8220700\",\"url\":\"www.intentsoft.com\",\"categories\":[\"company\",\"computer data services\"],\"classifications\":[{\"code\":\"COMPANY\",\"names\":[{\"nameLocale\":\"en-US\",\"name\":\"company\"},{\"nameLocale\":\"en-US\",\"name\":\"computer data services\"}]}]},\"address\":{\"streetNumber\":\"1\",\"streetName\":\"Microsoft Way\",\"municipality\":\"Redmond\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle East\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98052\",\"extendedPostalCode\":\"980526399\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"1 Microsoft Way, Redmond, WA 98052\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.63967,\"lon\":-122.13029},\"viewport\":{\"topLeftPoint\":{\"lat\":47.64057,\"lon\":-122.13162},\"btmRightPoint\":{\"lat\":47.63877,\"lon\":-122.12896}},\"entryPoints\":[{\"type\":\"main\",\"position\":{\"lat\":47.63962,\"lon\":-122.13029}}]},{\"type\":\"POI\",\"id\":\"US/POI/p0/4207658\",\"score\":-0.058,\"dist\":57.86137366890676,\"info\":\"search:ta:840539001255513-US\",\"poi\":{\"name\":\"Microsoft Corporation\",\"phone\":\"+(1)-(425)-4217900\",\"categories\":[\"electrical, office it: computer computer supplies\",\"shop\"],\"classifications\":[{\"code\":\"SHOP\",\"names\":[{\"nameLocale\":\"en-US\",\"name\":\"electrical, office it: computer computer supplies\"},{\"nameLocale\":\"en-US\",\"name\":\"shop\"}]}]},\"address\":{\"streetNumber\":\"3635\",\"streetName\":\"157th Ave NE\",\"municipality\":\"Redmond\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle East\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98052\",\"extendedPostalCode\":\"980525449\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"3635 157th Ave NE, Redmond, WA 98052\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.63966,\"lon\":-122.13138},\"viewport\":{\"topLeftPoint\":{\"lat\":47.64056,\"lon\":-122.13271},\"btmRightPoint\":{\"lat\":47.63876,\"lon\":-122.13005}},\"entryPoints\":[{\"type\":\"main\",\"position\":{\"lat\":47.63966,\"lon\":-122.1306}}]}]}";
        }

        private string GetSingleRouteDirection()
        {
            return "{  \"formatVersion\": \"0.0.12\",  \"copyright\": \"Copyright 2017 TomTom International BV. All rights reserved. This navigation data is the proprietary copyright of TomTom International BV and may be used only in accordance with the terms of a fully executed license agreement entered into between TomTom International BV, or an authorised reseller and yourself. If you have not entered into such a license agreement you are not authorised to use this data in any manner and should immediately return it to TomTom International BV.\",  \"privacy\": \"TomTom keeps information that tells us how and when you use our services. This includes information about the device you are using and the information we receive while you use the service, such as locations, routes, destinations and search queries. TomTom is unable to identify you based on the information it collects, and will not try to. TomTom uses the information for technical diagnostics, to detect fraud and abuse, to create usage reports, and to improve its services. The information is kept only for these purposes and for a limited period of time, after which it is destroyed. TomTom applies security methods based on industry standards to protect the information against unauthorised access. TomTom will not give anyone else access to the information or use it for any other purpose, unless explicitly and lawfully ordered to do so following due legal process. You can find out more at http://tomtom.com/privacy. You can contact TomTom by going to http://tomtom.com/support.\",  \"routes\": [    {      \"summary\": {        \"lengthInMeters\": 1147,        \"travelTimeInSeconds\": 162,        \"trafficDelayInSeconds\": 0,        \"departureTime\": \"2017-09-07T16:56:58+00:00\",        \"arrivalTime\": \"2017-09-07T16:59:40+00:00\"      },      \"legs\": [        {          \"summary\": {            \"lengthInMeters\": 1147,            \"travelTimeInSeconds\": 162,            \"trafficDelayInSeconds\": 0,            \"departureTime\": \"2017-09-07T16:56:58+00:00\",            \"arrivalTime\": \"2017-09-07T16:59:40+00:00\"        },          \"points\": [            {              \"latitude\": 52.50931,              \"logitude\": 13.42937            },            {              \"latitude\": 52.50904,              \"longitude\": 13.42912            },            {              \"latitude\": 52.50894,              \"longitude\": 13.42904            },            {              \"latitude\": 52.50867,              \"longitude\": 13.42879            },            {              \"latitude\": 52.5084,              \"longitude\": 13.42857            },            {              \"latitude\": 52.50791,              \"longitude\": 13.42824            },            {              \"latitude\": 52.50757,              \"longitude\": 13.42772            },            {              \"latitude\": 52.50735,              \"longitude\": 13.42823            },            {              \"latitude\": 52.5073,              \"longitude\": 13.42836            },            {              \"latitude\": 52.50573,              \"longitude\": 13.43194            },            {              \"latitude\": 52.50512,              \"longitude\": 13.43336            },            {              \"latitude\": 52.50464,              \"longitude\": 13.43451            },            {              \"latitude\": 52.5045,              \"longitude\": 13.43481            },            {              \"latitude\": 52.50443,              \"longitude\": 13.43498            },            {              \"latitude\": 52.50343,              \"longitude\": 13.43737            },            {              \"latitude\": 52.50274,              \"longitude\": 13.43872            }          ]        }      ],      \"sections\": [        {          \"startPointIndex\": 0,          \"endPointIndex\": 15,          \"sectionType\": \"TRAVEL_MODE\",          \"travelMode\": \"car\"        }      ]    }  ]}";
        }

        // same query with real subscription key
        private string GetMultipleRouteDirections()
        {
            return "{\"formatVersion\":\"0.0.12\",\"routes\":[{\"summary\":{\"lengthInMeters\":24,\"travelTimeInSeconds\":4,\"trafficDelayInSeconds\":0,\"departureTime\":\"2019-08-05T07:47:46+00:00\",\"arrivalTime\":\"2019-08-05T07:47:50+00:00\"},\"legs\":[{\"summary\":{\"lengthInMeters\":24,\"travelTimeInSeconds\":4,\"trafficDelayInSeconds\":0,\"departureTime\":\"2019-08-05T07:47:46+00:00\",\"arrivalTime\":\"2019-08-05T07:47:50+00:00\"},\"points\":[{\"latitude\":47.63962,\"longitude\":-122.13061},{\"latitude\":47.63962,\"longitude\":-122.13029}]}],\"sections\":[{\"startPointIndex\":0,\"endPointIndex\":1,\"sectionType\":\"TRAVEL_MODE\",\"travelMode\":\"car\"}],\"guidance\":{\"instructions\":[{\"routeOffsetInMeters\":0,\"travelTimeInSeconds\":0,\"point\":{\"latitude\":47.63962,\"longitude\":-122.13061},\"instructionType\":\"LOCATION_DEPARTURE\",\"street\":\"Microsoft Way\",\"possibleCombineWithNext\":false,\"drivingSide\":\"RIGHT\",\"maneuver\":\"DEPART\",\"message\":\"Leave from Microsoft Way\"},{\"routeOffsetInMeters\":24,\"travelTimeInSeconds\":4,\"point\":{\"latitude\":47.63962,\"longitude\":-122.13029},\"instructionType\":\"LOCATION_ARRIVAL\",\"street\":\"Microsoft Way\",\"possibleCombineWithNext\":false,\"drivingSide\":\"RIGHT\",\"maneuver\":\"ARRIVE_LEFT\",\"message\":\"You have arrived at Microsoft Way. Your destination is on the left\"}],\"instructionGroups\":[{\"firstInstructionIndex\":0,\"lastInstructionIndex\":1,\"groupMessage\":\"Leave from Microsoft Way. Continue to your destination at Microsoft Way\",\"groupLengthInMeters\":24}]}},{\"summary\":{\"lengthInMeters\":24,\"travelTimeInSeconds\":4,\"trafficDelayInSeconds\":0,\"departureTime\":\"2019-08-05T07:47:46+00:00\",\"arrivalTime\":\"2019-08-05T07:47:50+00:00\"},\"legs\":[{\"summary\":{\"lengthInMeters\":24,\"travelTimeInSeconds\":4,\"trafficDelayInSeconds\":0,\"departureTime\":\"2019-08-05T07:47:46+00:00\",\"arrivalTime\":\"2019-08-05T07:47:50+00:00\"},\"points\":[{\"latitude\":47.63962,\"longitude\":-122.13061},{\"latitude\":47.63962,\"longitude\":-122.13029}]}],\"sections\":[{\"startPointIndex\":0,\"endPointIndex\":1,\"sectionType\":\"TRAVEL_MODE\",\"travelMode\":\"car\"}],\"guidance\":{\"instructions\":[{\"routeOffsetInMeters\":0,\"travelTimeInSeconds\":0,\"point\":{\"latitude\":47.63962,\"longitude\":-122.13061},\"instructionType\":\"LOCATION_DEPARTURE\",\"street\":\"Microsoft Way\",\"possibleCombineWithNext\":false,\"drivingSide\":\"RIGHT\",\"maneuver\":\"DEPART\",\"message\":\"Leave from Microsoft Way\"},{\"routeOffsetInMeters\":24,\"travelTimeInSeconds\":4,\"point\":{\"latitude\":47.63962,\"longitude\":-122.13029},\"instructionType\":\"LOCATION_ARRIVAL\",\"street\":\"Microsoft Way\",\"possibleCombineWithNext\":false,\"drivingSide\":\"RIGHT\",\"maneuver\":\"ARRIVE_LEFT\",\"message\":\"You have arrived at Microsoft Way. Your destination is on the left\"}],\"instructionGroups\":[{\"firstInstructionIndex\":0,\"lastInstructionIndex\":1,\"groupMessage\":\"Leave from Microsoft Way. Continue to your destination at Microsoft Way\",\"groupLengthInMeters\":24}]}}]}";
        }

        private string GetAddressSearch()
        {
            return "{\"summary\":{\"query\":\"1635 11th ave\",\"queryType\":\"NON_NEAR\",\"queryTime\":6,\"numResults\":3,\"offset\":0,\"totalResults\":196,\"fuzzyLevel\":1,\"geoBias\":{\"lat\":47.63962,\"lon\":-122.13061}},\"results\":[{\"type\":\"Address Range\",\"id\":\"US/ADDR/p0/9302193\",\"score\":6.917,\"dist\":11511.852754030037,\"address\":{\"streetNumber\":\"1635\",\"streetName\":\"11th Avenue Northwest\",\"municipalitySubdivision\":\"Issaquah\",\"municipality\":\"Issaquah\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Issaquah Plateau\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98027\",\"extendedPostalCode\":\"980275323\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"1635 11th Avenue Northwest, Issaquah, WA 98027\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.54994,\"lon\":-122.05415},\"viewport\":{\"topLeftPoint\":{\"lat\":47.54972,\"lon\":-122.05415},\"btmRightPoint\":{\"lat\":47.54994,\"lon\":-122.05422}},\"addressRanges\":{\"rangeLeft\":\"1633 - 1675\",\"rangeRight\":\"1632 - 1674\",\"from\":{\"lat\":47.54994,\"lon\":-122.05422},\"to\":{\"lat\":47.54972,\"lon\":-122.05415}}},{\"type\":\"Point Address\",\"id\":\"US/PAD/p1/47865447\",\"score\":6.592,\"dist\":18095.899403431307,\"address\":{\"streetNumber\":\"1635\",\"streetName\":\"11th Avenue West\",\"municipalitySubdivision\":\"West Queen Anne, Seattle\",\"municipality\":\"Seattle\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98119\",\"extendedPostalCode\":\"981192903\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"1635 11th Avenue West, Seattle, WA 98119\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.63455,\"lon\":-122.37201},\"viewport\":{\"topLeftPoint\":{\"lat\":47.63545,\"lon\":-122.37334},\"btmRightPoint\":{\"lat\":47.63365,\"lon\":-122.37068}},\"entryPoints\":[{\"type\":\"main\",\"position\":{\"lat\":47.63454,\"lon\":-122.37166}}]},{\"type\":\"Street\",\"id\":\"US/STR/p0/9088805\",\"score\":5.472,\"dist\":7012.489516609716,\"address\":{\"streetName\":\"11th Avenue\",\"municipalitySubdivision\":\"Norkirk, Kirkland\",\"municipality\":\"Kirkland\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle East\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98033\",\"extendedPostalCode\":\"980335405,980335519,980335600,980335611\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"11th Avenue, Kirkland, WA 98033\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.68395,\"lon\":-122.19721},\"viewport\":{\"topLeftPoint\":{\"lat\":47.67488,\"lon\":-122.18839},\"btmRightPoint\":{\"lat\":47.69286,\"lon\":-122.21511}}}]}";
        }

        private string GetAzureMapsParkingCategory()
        {
            return "{\"summary\":{\"query\":\"open_parking_area parking_garage\",\"queryType\":\"NON_NEAR\",\"queryTime\":76,\"numResults\":3,\"offset\":0,\"totalResults\":1550,\"fuzzyLevel\":1,\"geoBias\":{\"lat\":47.63455,\"lon\":-122.37201}},\"results\":[{\"type\":\"POI\",\"id\":\"US/POI/p1/4035442\",\"score\":5.664,\"dist\":620.133000563444,\"info\":\"search:ta:840539000698502-US\",\"poi\":{\"name\":\"1110 Elliott Avenue West\",\"phone\":\"+(1)-(206)-2846303\",\"brand\":\"Diamond Parking Service\",\"categories\":[\"open parking area\"],\"classifications\":[{\"code\":\"OPEN_PARKING_AREA\",\"names\":[{\"nameLocale\":\"en-US\",\"name\":\"open parking area\"}]}]},\"address\":{\"streetNumber\":\"1110\",\"streetName\":\"Elliott Ave W\",\"municipalitySubdivision\":\"West Queen Anne, Seattle\",\"municipality\":\"Seattle\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98119\",\"extendedPostalCode\":\"981193102\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"1110 Elliott Ave W, Seattle, WA 98119\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.62903,\"lon\":-122.37083},\"viewport\":{\"topLeftPoint\":{\"lat\":47.62993,\"lon\":-122.37216},\"btmRightPoint\":{\"lat\":47.62813,\"lon\":-122.3695}},\"entryPoints\":[{\"type\":\"main\",\"position\":{\"lat\":47.62888,\"lon\":-122.3714}}]},{\"type\":\"POI\",\"id\":\"US/POI/p0/3546702\",\"score\":5.664,\"dist\":627.2016847474632,\"info\":\"search:ta:840539001936436-US\",\"poi\":{\"name\":\"1108 Elliott Ave W\",\"categories\":[\"open parking area\"],\"classifications\":[{\"code\":\"OPEN_PARKING_AREA\",\"names\":[{\"nameLocale\":\"en-US\",\"name\":\"open parking area\"}]}]},\"address\":{\"streetNumber\":\"1108\",\"streetName\":\"Elliott Ave W\",\"municipalitySubdivision\":\"West Queen Anne, Seattle\",\"municipality\":\"Seattle\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98119\",\"extendedPostalCode\":\"981193102\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"1108 Elliott Ave W, Seattle, WA 98119\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.62894,\"lon\":-122.37114},\"viewport\":{\"topLeftPoint\":{\"lat\":47.62984,\"lon\":-122.37247},\"btmRightPoint\":{\"lat\":47.62804,\"lon\":-122.36981}},\"entryPoints\":[{\"type\":\"main\",\"position\":{\"lat\":47.62937,\"lon\":-122.37202}}]},{\"type\":\"POI\",\"id\":\"US/POI/p0/4569598\",\"score\":5.663,\"dist\":991.4432801144699,\"info\":\"search:ta:840539000670354-US\",\"poi\":{\"name\":\"660 Elliott Avenue West\",\"phone\":\"+(1)-(206)-2843100\",\"brand\":\"Diamond Parking Service\",\"categories\":[\"open parking area\"],\"classifications\":[{\"code\":\"OPEN_PARKING_AREA\",\"names\":[{\"nameLocale\":\"en-US\",\"name\":\"open parking area\"}]}]},\"address\":{\"streetNumber\":\"660\",\"streetName\":\"Elliott Ave W\",\"municipalitySubdivision\":\"Seattle, Lower Queen Anne\",\"municipality\":\"Seattle\",\"countrySecondarySubdivision\":\"King\",\"countryTertiarySubdivision\":\"Seattle\",\"countrySubdivision\":\"WA\",\"postalCode\":\"98119\",\"extendedPostalCode\":\"981193898\",\"countryCode\":\"US\",\"country\":\"United States Of America\",\"countryCodeISO3\":\"USA\",\"freeformAddress\":\"660 Elliott Ave W, Seattle, WA 98119\",\"countrySubdivisionName\":\"Washington\"},\"position\":{\"lat\":47.62619,\"lon\":-122.36741},\"viewport\":{\"topLeftPoint\":{\"lat\":47.62709,\"lon\":-122.36874},\"btmRightPoint\":{\"lat\":47.62529,\"lon\":-122.36608}},\"entryPoints\":[{\"type\":\"main\",\"position\":{\"lat\":47.62606,\"lon\":-122.36762}}]}]}";
        }

        // foursquare
        private string GetFoursquarePointOfInterest()
        {
            return "{  \"meta\": {    \"code\": 200,    \"requestId\": \"59a45921351e3d43b07028b5\"  }, \"response\": {    \"venue\": { \"id\": \"412d2800f964a520df0c1fe3\", \"name\": \"Central Park\",      \"contact\": {        \"phone\": \"2123106600\",        \"formattedPhone\": \"(212) 310-6600\", \"twitter\": \"centralparknyc\", \"instagram\": \"centralparknyc\", \"facebook\": \"37965424481\", \"facebookUsername\": \"centralparknyc\", \"facebookName\": \"Central Park\" }, \"location\": { \"address\": \"59th St to 110th St\", \"crossStreet\": \"5th Ave to Central Park West\", \"lat\": 40.78408342593807, \"lng\": -73.96485328674316, \"postalCode\": \"10028\", \"cc\": \"US\", \"city\": \"New York\", \"state\": \"NY\", \"country\": \"United States\", \"formattedAddress\": [ \"59th St to 110th St (5th Ave to Central Park West)\", \"New York, NY 10028\", \"United States\" ] }, \"canonicalUrl\": \"https://foursquare.com/v/central-park/412d2800f964a520df0c1fe3\", \"categories\": [ { \"id\": \"4bf58dd8d48988d163941735\", \"name\": \"Park\", \"pluralName\": \"Parks\", \"shortName\": \"Park\", \"icon\": { \"prefix\": \"https://ss3.4sqi.net/img/categories_v2/parks_outdoors/park_\", \"suffix\": \".png\" }, \"primary\": true } ], \"verified\": true, \"stats\": { \"checkinsCount\": 364591, \"usersCount\": 311634, \"tipCount\": 1583, \"visitsCount\": 854553 }, \"url\": \"http://www.centralparknyc.org\", \"likes\": { \"count\": 17370, \"summary\": \"17370 Likes\" }, \"rating\": 9.8, \"ratingColor\": \"00B551\", \"ratingSignals\": 18854, \"beenHere\": { \"count\": 0, \"unconfirmedCount\": 0, \"marked\": false, \"lastCheckinExpiredAt\": 0 }, \"photos\": { \"count\": 26681, \"groups\": [ { \"type\": \"venue\", \"name\": \"Venue photos\", \"count\": 26681, \"items\": [ { \"id\": \"513bd223e4b0e8ef8292ee54\", \"createdAt\": 1362874915, \"source\": { \"name\": \"Instagram\", \"url\": \"http://instagram.com\" }, \"prefix\": \"https://igx.4sqi.net/img/general/\", \"suffix\": \"/655018_Zp3vA90Sy4IIDApvfAo5KnDItoV0uEDZeST7bWT-qzk.jpg\", \"width\": 612, \"height\": 612, \"user\": { \"id\": \"123456\", \"firstName\": \"John\", \"lastName\": \"Doe\", \"gender\": \"male\" }, \"visibility\": \"public\" } ] } ] }, \"description\": \"Central Park is the 843-acre green heart of Manhattan and is maintained by the Central Park Conservancy. It was designed in the 19th century by Frederick Law Olmsted and Calvert Vaux as an urban escape for New Yorkers, and now receives over 40 million visits per year.\", \"storeId\": \"\", \"page\": { \"pageInfo\": { \"description\": \"The mission of the Central Park Conservancy, a private non-profit, is to restore, manage, and enhance Central Park, in partnership with the public.\", \"banner\": \"https://is1.4sqi.net/userpix/HS2JAA2IAAAR2WZO.jpg\", \"links\": { \"count\": 1, \"items\": [ { \"url\": \"http://www.centralparknyc.org\" } ] } }, \"user\": { \"id\": \"29060351\", \"firstName\": \"Central Park\", \"gender\": \"none\", \"photo\": { \"prefix\": \"https://igx.4sqi.net/img/user/\", \"suffix\": \"/PCPGGJ2N3ULA5O05.jpg\" }, \"type\": \"chain\", \"tips\": { \"count\": 37 }, \"lists\": { \"groups\": [ { \"type\": \"created\", \"count\": 2, \"items\": [] } ] }, \"homeCity\": \"New York, NY\", \"bio\": \"\", \"contact\": { \"twitter\": \"centralparknyc\", \"facebook\": \"37965424481\" } } }, \"hereNow\": { \"count\": 16, \"summary\": \"16 people are here\", \"groups\": [ { \"type\": \"others\", \"name\": \"Other people here\", \"count\": 16, \"items\": [] } ] }, \"createdAt\": 1093478400, \"tips\": { \"count\": 1583, \"groups\": [ { \"type\": \"others\", \"name\": \"All tips\", \"count\": 1583, \"items\": [ { \"id\": \"5150464ee4b02f70eb28eee4\", \"createdAt\": 1364215374, \"text\": \"Did you know? To create that feeling of being in the countryside, and not in the middle of a city, the four Transverse Roads were sunken down eight feet below the park’s surface.\", \"type\": \"user\", \"canonicalUrl\": \"https://foursquare.com/item/5150464ee4b02f70eb28eee4\", \"photo\": { \"id\": \"5150464f52625adbe29d04c2\", \"createdAt\": 1364215375, \"source\": { \"name\": \"Foursquare Web\", \"url\": \"https://foursquare.com\" }, \"prefix\": \"https://igx.4sqi.net/img/general/\", \"suffix\": \"/13764780_Ao02DfJpgG1ar2PfgP51hOKWsn38iai8bsSpzKd0GcM.jpg\", \"width\": 800, \"height\": 542, \"visibility\": \"public\" }, \"photourl\": \"https://igx.4sqi.net/img/general/original/13764780_Ao02DfJpgG1ar2PfgP51hOKWsn38iai8bsSpzKd0GcM.jpg\", \"lang\": \"en\", \"likes\": { \"count\": 247, \"groups\": [ { \"type\": \"others\", \"count\": 247, \"items\": [] } ], \"summary\": \"247 likes\" }, \"logView\": true, \"agreeCount\": 246, \"disagreeCount\": 0, \"todo\": { \"count\": 30 }, \"user\": { \"id\": \"13764780\", \"firstName\": \"City of New York\", \"gender\": \"none\", \"photo\": { \"prefix\": \"https://igx.4sqi.net/img/user/\", \"suffix\": \"/2X1FKJPUY3DGRRK3.png\" }, \"type\": \"page\" } }, { \"id\": \"522afa5b11d2740e9aeeb336\", \"createdAt\": 1378548315, \"text\": \"Lots of squirrels in the park! パーク内にはリスがたくさんいます！しかも思ったよりデカイです。\", \"type\": \"user\", \"logView\": true, \"editedAt\": 1399418942, \"agreeCount\": 61, \"disagreeCount\": 0, \"todo\": { \"count\": 1 }, \"user\": { \"id\": \"5053872\", \"firstName\": \"Nnkoji\", \"gender\": \"male\", \"photo\": { \"prefix\": \"https://igx.4sqi.net/img/user/\", \"suffix\": \"/5053872-DUZ51RAOUVH3GU33.jpg\" } }, \"authorInteractionType\": \"liked\" }, { \"id\": \"4cd5bda1b6962c0fd19c2e96\", \"createdAt\": 1289076129, \"text\": \"PHOTO: 1975 was the last year the New York City marathon was raced entirely inside Central Park. In this photo, runners at the marathon starting line.\", \"type\": \"user\", \"url\": \"http://www.nydailynewspix.com/sales/largeview.php?name=87g0km0g.jpg&id=152059&lbx=-1&return_page=searchResults.php&page=2\", \"canonicalUrl\": \"https://foursquare.com/item/4cd5bda1b6962c0fd19c2e96\", \"lang\": \"en\", \"likes\": { \"count\": 26, \"groups\": [ { \"type\": \"others\", \"count\": 26, \"items\": [] } ], \"summary\": \"26 likes\" }, \"logView\": true, \"agreeCount\": 25, \"disagreeCount\": 0, \"todo\": { \"count\": 16 }, \"user\": { \"id\": \"1241858\", \"firstName\": \"The New York Daily News\", \"gender\": \"none\", \"photo\": { \"prefix\": \"https://igx.4sqi.net/img/user/\", \"suffix\": \"/3EV01452MGIUWBAQ.jpg\" }, \"type\": \"page\" } } ] } ] }, \"shortUrl\": \"http://4sq.com/2UsPUp\", \"timeZone\": \"America/New_York\", \"listed\": { \"count\": 5731, \"groups\": [ { \"type\": \"others\", \"name\": \"Lists from other people\", \"count\": 5731, \"items\": [ { \"id\": \"4fad24a2e4b0bcc0c18be03c\", \"name\": \"101 places to see in Manhattan before you die\", \"description\": \"Best spots to see in Manhattan (New York City) as restaurants, monuments and public spaces. Enjoy!\", \"type\": \"others\", \"user\": { \"id\": \"356747\", \"firstName\": \"John\", \"lastName\": \"Doe\", \"gender\": \"male\", \"photo\": { \"prefix\": \"https://igx.4sqi.net/img/user/\", \"suffix\": \"/356747-WQOTM2ASOIERONL3.jpg\" } }, \"editable\": false, \"public\": true, \"collaborative\": false, \"url\": \"/boke/list/101-places-to-see-in-manhattan-before-you-die\", \"canonicalUrl\": \"https://foursquare.com/boke/list/101-places-to-see-in-manhattan-before-you-die\", \"createdAt\": 1336747170, \"updatedAt\": 1406242886, \"photo\": { \"id\": \"4fa97b0c121d8a3faef6f2df\", \"createdAt\": 1336507148, \"prefix\": \"https://igx.4sqi.net/img/general/\", \"suffix\": \"/IcmBihQCVr4Zt0Vxt9l237NHv--nxg1Z5_8QIMjeD8E.jpg\", \"width\": 325, \"height\": 487, \"user\": { \"id\": \"13125997\", \"firstName\": \"IWalked Audio Tours\", \"gender\": \"none\", \"photo\": { \"prefix\": \"https://igx.4sqi.net/img/user/\", \"suffix\": \"/KZCTVBJ0FXUHSQA5.jpg\" }, \"type\": \"page\" }, \"visibility\": \"public\" }, \"followers\": { \"count\": 944 }, \"listItems\": { \"count\": 101, \"items\": [ { \"id\": \"t4b67904a70c603bb845291b4\", \"createdAt\": 1336747293, \"photo\": { \"id\": \"4faa9dd9e4b01bd5523d1de8\", \"createdAt\": 1336581593, \"prefix\": \"https://igx.4sqi.net/img/general/\", \"suffix\": \"/KaAuGPKMZev1Te0uucRYHk92RiULGj3-GYWkX_zXbjM.jpg\", \"width\": 720, \"height\": 532, \"visibility\": \"public\" } } ] } } ] } ] }, \"phrases\": [ { \"phrase\": \"parque todo\", \"sample\": { \"entities\": [ { \"indices\": [ 22, 33 ], \"type\": \"keyPhrase\" } ], \"text\": \"... a ponta, curtir o parque todo, sem pressa, admirando cada lugar. Se puder...\" }, \"count\": 4 } ], \"hours\": { \"status\": \"Open until 1:00 AM\", \"isOpen\": true, \"isLocalHoliday\": false, \"timeframes\": [ { \"days\": \"Mon–Sun\", \"includesToday\": true, \"open\": [ { \"renderedTime\": \"6:00 AM–1:00 AM\" } ], \"segments\": [] } ] }, \"popular\": { \"status\": \"Likely open\", \"isOpen\": true, \"isLocalHoliday\": false, \"timeframes\": [ { \"days\": \"Tue–Thu\", \"open\": [ { \"renderedTime\": \"Noon–8:00 PM\" } ], \"segments\": [] }, { \"days\": \"Fri\", \"open\": [ { \"renderedTime\": \"11:00 AM–7:00 PM\" } ], \"segments\": [] }, { \"days\": \"Sat\", \"open\": [ { \"renderedTime\": \"8:00 AM–8:00 PM\" } ], \"segments\": [] }, { \"days\": \"Sun\", \"open\": [ { \"renderedTime\": \"8:00 AM–7:00 PM\" } ], \"segments\": [] } ] }, \"pageUpdates\": { \"count\": 12, \"items\": [] }, \"inbox\": { \"count\": 0, \"items\": [] }, \"venueChains\": [], \"attributes\": { \"groups\": [ { \"type\": \"payments\", \"name\": \"Credit Cards\", \"summary\": \"No Credit Cards\", \"count\": 7, \"items\": [ { \"displayName\": \"Credit Cards\", \"displayValue\": \"No\" } ] } ] }, \"bestPhoto\": { \"id\": \"513bd223e4b0e8ef8292ee54\", \"createdAt\": 1362874915, \"source\": { \"name\": \"Instagram\", \"url\": \"http://instagram.com\" }, \"prefix\": \"https://igx.4sqi.net/img/general/\", \"suffix\": \"/655018_Zp3vA90Sy4IIDApvfAo5KnDItoV0uEDZeST7bWT-qzk.jpg\", \"width\": 612, \"height\": 612, \"visibility\": \"public\" } } } } ";
        }

        // same query without query parameter with real subscription key. then name is changed for testing
        private string GetFoursquareExplore()
        {
            return "{\"meta\":{\"code\":200,\"requestId\":\"5dc93ef3b42667002cf29dcb\"},\"response\":{\"suggestedFilters\":{\"header\":\"Tap to show:\",\"filters\":[{\"name\":\"Open now\",\"key\":\"openNow\"},{\"name\":\"$-$$$$\",\"key\":\"price\"}]},\"warning\":{\"text\":\"There aren't a lot of results near you. Try something more general, reset your filters, or expand the search area.\"},\"headerLocation\":\"Redmond\",\"headerFullLocation\":\"Redmond\",\"headerLocationGranularity\":\"city\",\"totalResults\":242,\"suggestedBounds\":{\"ne\":{\"lat\":47.86462022500022,\"lng\":-121.79730200261386},\"sw\":{\"lat\":47.41461977499978,\"lng\":-122.46391799738615}},\"groups\":[{\"type\":\"Recommended Places\",\"name\":\"recommended\",\"items\":[{\"reasons\":{\"count\":0,\"items\":[{\"summary\":\"This spot is popular\",\"type\":\"general\",\"reasonName\":\"globalInteractionReason\"}]},\"venue\":{\"id\":\"41351100f964a520bd1a1fe3\",\"name\":\"Pro Sports Club\",\"location\":{\"address\":\"4455 148th Ave NE\",\"crossStreet\":\"btwn NE 45th & 46th St\",\"lat\":47.65175455426816,\"lng\":-122.14504565674004,\"labeledLatLngs\":[{\"label\":\"display\",\"lat\":47.65175455426816,\"lng\":-122.14504565674004}],\"distance\":1731,\"postalCode\":\"98007\",\"cc\":\"US\",\"city\":\"Bellevue\",\"state\":\"WA\",\"country\":\"United States\",\"formattedAddress\":[\"4455 148th Ave NE (btwn NE 45th & 46th St)\",\"Bellevue, WA 98007\",\"United States\"]},\"categories\":[{\"id\":\"4bf58dd8d48988d176941735\",\"name\":\"Gym\",\"pluralName\":\"Gyms\",\"shortName\":\"Gym\",\"icon\":{\"prefix\":\"https://ss3.4sqi.net/img/categories_v2/building/gym_\",\"suffix\":\".png\"},\"primary\":true}],\"photos\":{\"count\":0,\"groups\":[]}},\"referralId\":\"e-0-41351100f964a520bd1a1fe3-0\"},{\"reasons\":{\"count\":0,\"items\":[{\"summary\":\"This spot is popular\",\"type\":\"general\",\"reasonName\":\"globalInteractionReason\"}]},\"venue\":{\"id\":\"4e18b1fdae6092c27654e947\",\"name\":\"Marymoor Dog Park\",\"location\":{\"address\":\"West Lake Sammamish Pkwy\",\"lat\":47.65948238278925,\"lng\":-122.11769355852032,\"labeledLatLngs\":[{\"label\":\"display\",\"lat\":47.65948238278925,\"lng\":-122.11769355852032}],\"distance\":2413,\"postalCode\":\"98052\",\"cc\":\"US\",\"city\":\"Redmond\",\"state\":\"WA\",\"country\":\"United States\",\"formattedAddress\":[\"West Lake Sammamish Pkwy\",\"Redmond, WA 98052\",\"United States\"]},\"categories\":[{\"id\":\"4bf58dd8d48988d1e5941735\",\"name\":\"Dog Run\",\"pluralName\":\"Dog Runs\",\"shortName\":\"Dog Run\",\"icon\":{\"prefix\":\"https://ss3.4sqi.net/img/categories_v2/parks_outdoors/dogrun_\",\"suffix\":\".png\"},\"primary\":true}],\"photos\":{\"count\":0,\"groups\":[]}},\"referralId\":\"e-0-4e18b1fdae6092c27654e947-1\"},{\"reasons\":{\"count\":0,\"items\":[{\"summary\":\"This spot is popular\",\"type\":\"general\",\"reasonName\":\"globalInteractionReason\"}]},\"venue\":{\"id\":\"54629ce7498ecce55ba77f3d\",\"name\":\"Marymoor Dog Park\",\"location\":{\"address\":\"13310 Bel Red Rd\",\"crossStreet\":\"132nd\",\"lat\":47.62266892940679,\"lng\":-122.16278133795399,\"labeledLatLngs\":[{\"label\":\"display\",\"lat\":47.62266892940679,\"lng\":-122.16278133795399}],\"distance\":3063,\"postalCode\":\"98005\",\"cc\":\"US\",\"city\":\"Bellevue\",\"state\":\"WA\",\"country\":\"United States\",\"formattedAddress\":[\"13310 Bel Red Rd (132nd)\",\"Bellevue, WA 98005\",\"United States\"]},\"categories\":[{\"id\":\"4bf58dd8d48988d1f3941735\",\"name\":\"Toy / Game Store\",\"pluralName\":\"Toy / Game Stores\",\"shortName\":\"Toys & Games\",\"icon\":{\"prefix\":\"https://ss3.4sqi.net/img/categories_v2/shops/toys_\",\"suffix\":\".png\"},\"primary\":true}],\"photos\":{\"count\":0,\"groups\":[]},\"venuePage\":{\"id\":\"104601850\"}},\"referralId\":\"e-0-54629ce7498ecce55ba77f3d-2\"}]}]}}";
        }

        private string GetFoursquareParkingCategory()
        {
            return "{\"meta\":{\"code\":200,\"requestId\":\"5c59ce724434b9740effd0ab\"},\"response\":{\"venues\":[{\"id\":\"4d9b3f79335ab60c934cf6f9\",\"name\":\"Sea-Tac Airport Parking Garage\",\"contact\":{\"phone\":\"2067875308\",\"formattedPhone\":\"(206) 787-5308\",\"twitter\":\"seatacairport\",\"facebook\":\"126440017409096\",\"facebookUsername\":\"seatacairport\",\"facebookName\":\"Seattle-Tacoma International Airport (Sea-Tac)\"},\"location\":{\"address\":\"17801 International Blvd\",\"lat\":47.44331445343872,\"lng\":-122.30100235715727,\"labeledLatLngs\":[{\"label\":\"display\",\"lat\":47.44331445343872,\"lng\":-122.30100235715727}],\"distance\":21946,\"postalCode\":\"98158\",\"cc\":\"US\",\"city\":\"SeaTac\",\"state\":\"WA\",\"country\":\"United States\",\"formattedAddress\":[\"17801 International Blvd\",\"SeaTac, WA 98158\",\"United States\"]},\"categories\":[{\"id\":\"4c38df4de52ce0d596b336e1\",\"name\":\"Parking\",\"pluralName\":\"Parking\",\"shortName\":\"Parking\",\"icon\":{\"prefix\":\"https:\\/\\/ss3.4sqi.net\\/img\\/categories_v2\\/building\\/parking_\",\"suffix\":\".png\"},\"primary\":true}],\"verified\":true,\"stats\":{\"tipCount\":14,\"usersCount\":2851,\"checkinsCount\":8075},\"url\":\"http:\\/\\/www.portseattle.org\\/Sea-Tac\\/Parking-and-Transportation\\/Parking\",\"venueRatingBlacklisted\":true,\"allowMenuUrlEdit\":true,\"beenHere\":{\"lastCheckinExpiredAt\":0},\"specials\":{\"count\":0,\"items\":[]},\"storeId\":\"\",\"referralId\":\"v-1549389426\",\"venueChains\":[{\"id\":\"556e58ddbd6a82902e28f35d\"}],\"hasPerk\":false},{\"id\":\"4e6fb11cd1647b113810c7e2\",\"name\":\"Bellevue Square Parking Garage\",\"contact\":{},\"location\":{\"address\":\"575 Bellevue Sq\",\"crossStreet\":\"at Bellevue Square\",\"lat\":47.61623993523633,\"lng\":-122.20523033840433,\"labeledLatLngs\":[{\"label\":\"display\",\"lat\":47.61623993523633,\"lng\":-122.20523033840433}],\"distance\":12677,\"postalCode\":\"98004\",\"cc\":\"US\",\"city\":\"Bellevue\",\"state\":\"WA\",\"country\":\"United States\",\"formattedAddress\":[\"575 Bellevue Sq (at Bellevue Square)\",\"Bellevue, WA 98004\",\"United States\"]},\"categories\":[{\"id\":\"4c38df4de52ce0d596b336e1\",\"name\":\"Parking\",\"pluralName\":\"Parking\",\"shortName\":\"Parking\",\"icon\":{\"prefix\":\"https:\\/\\/ss3.4sqi.net\\/img\\/categories_v2\\/building\\/parking_\",\"suffix\":\".png\"},\"primary\":true}],\"verified\":false,\"stats\":{\"tipCount\":3,\"usersCount\":832,\"checkinsCount\":1900},\"venueRatingBlacklisted\":true,\"beenHere\":{\"lastCheckinExpiredAt\":0},\"specials\":{\"count\":0,\"items\":[]},\"referralId\":\"v-1549389426\",\"venueChains\":[],\"hasPerk\":false},{\"id\":\"4aca5bd2f964a52096c120e3\",\"name\":\"Sea-Tac Cell Phone Lot\",\"contact\":{\"twitter\":\"seatacairport\",\"facebook\":\"126440017409096\",\"facebookUsername\":\"seatacairport\",\"facebookName\":\"Seattle-Tacoma International Airport (Sea-Tac)\"},\"location\":{\"address\":\"2623 S 170th St\",\"lat\":47.44998114208292,\"lng\":-122.29912111008619,\"labeledLatLngs\":[{\"label\":\"display\",\"lat\":47.44998114208292,\"lng\":-122.29912111008619}],\"distance\":21263,\"postalCode\":\"98158\",\"cc\":\"US\",\"city\":\"SeaTac\",\"state\":\"WA\",\"country\":\"United States\",\"formattedAddress\":[\"2623 S 170th St\",\"SeaTac, WA 98158\",\"United States\"]},\"categories\":[{\"id\":\"4c38df4de52ce0d596b336e1\",\"name\":\"Parking\",\"pluralName\":\"Parking\",\"shortName\":\"Parking\",\"icon\":{\"prefix\":\"https:\\/\\/ss3.4sqi.net\\/img\\/categories_v2\\/building\\/parking_\",\"suffix\":\".png\"},\"primary\":true}],\"verified\":true,\"stats\":{\"tipCount\":76,\"usersCount\":5735,\"checkinsCount\":9931},\"url\":\"http:\\/\\/www.portseattle.org\\/Sea-Tac\\/Parking-and-Transportation\\/Parking\",\"venueRatingBlacklisted\":true,\"beenHere\":{\"lastCheckinExpiredAt\":0},\"specials\":{\"count\":0,\"items\":[]},\"storeId\":\"\",\"referralId\":\"v-1549389426\",\"venueChains\":[{\"id\":\"556e58ddbd6a82902e28f35d\"}],\"hasPerk\":false}]}}";
        }
    }
}
