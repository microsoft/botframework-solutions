using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestaurantBooking.Dialogs.RestaurantBooking.Resources;
using RestaurantBooking.Helpers;
using RestaurantBooking.Models;

namespace RestaurantBooking.SimulatedData
{
    public static class SeedReservationSampleData
    {
        private static IUrlResolver _urlResolver;

        public static List<BookingPlace> GetListOfRestaurants(string type, string location, IUrlResolver urlResolver)
        {
            _urlResolver = urlResolver;

            var list = new List<BookingPlace>();
            switch (type.ToLower())
            {
                case "chinese":
                    PopulateChineseRestaurants(location, list);
                    break;
                case "german":
                    PopulateGermanRestaurants(location, list);
                    break;
                case "italian":
                    PopulateItalianRestaurants(location, list);
                    break;
                case "indian":
                    PopulateIndianRestaurants(location, list);
                    break;
            }

            return list;
        }

        public static List<BookingPlace> GetListOfDefaultFoodTypes()
        {
            var list = new List<BookingPlace>
            {
                new BookingPlace { Category = "Chinese" },
                new BookingPlace { Category = "Italian" }
            };

            return list;
        }

        private static void PopulateIndianRestaurants(string location, List<BookingPlace> list)
        {
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Biryani House",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsBiryaniHouse),
                Category = "Indian"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Kanishka Cuisine",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsKanishkaCuisine),
                Category = "Indian"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Mayuri Indian",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsMaharaniInside),
                Category = "Indian"
            });
        }

        private static void PopulateItalianRestaurants(string location, List<BookingPlace> list)
        {
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Tony's",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsTonys),
                Category = "Italian"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Tuscani Grill",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsTuscaniGrill),
                Category = "Italian"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Mamma Mia Pizza",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsMammaMiaPizza),
                Category = "Italian"
            });
        }

        private static void PopulateGermanRestaurants(string location, List<BookingPlace> list)
        {
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Euro Bistro",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsEuroBistro),
                Category = "German"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "The Bavarian",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsTheBavarian),
                Category = "German"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "German Gourmet",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsGermanGourmet),
                Category = "German"
            });
        }

        private static void PopulateChineseRestaurants(string location, List<BookingPlace> list)
        {
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Chen's",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsChens),
                Category = "Chinese"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Bamboo Garden",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsBambooGarden),
                Category = "Chinese"
            });
            list.Add(new BookingPlace
            {
                Location = location,
                Name = "Mandarin",
                PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.RestaurantsMandarin),
                Category = "Chinese"
            });
        }
    }
}