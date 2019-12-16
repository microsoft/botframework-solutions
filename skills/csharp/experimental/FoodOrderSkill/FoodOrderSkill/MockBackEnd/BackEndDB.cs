using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FoodOrderSkill.MockBackEnd
{
    public class BackEndDB
    {
        public User[] UserList;
        public BackEndDB()
        {
            this.UserList = new User[]
            {
                new User(1, "Tom Winston", new FavoriteOrder[] {new FavoriteOrder("Sushi Maki", "Sushi", "555 albertson rd.", new string[] { "Shrimp Tempura role", "Seaweed salad", "Miso Soup", "Green Tea" }, 22.50), new FavoriteOrder("Burger Fly", "Burger", "555 albertson rd.", new string[] { "Double Cheeseburger", "Large Fries", "Large Coke" }, 14.50), new FavoriteOrder("Indian 2 Go", "Indian", "555 albertson rd.", new string[] { "Chicken Tika Masala", "Jazmine Rice", "Mango Chutney" }, 21.36) }),
            };
        }
    }

    public class User
    {
        public int Id;
        public string Name;
        public FavoriteOrder[] FavoriteOrders;

        public User(int id, string name, FavoriteOrder[] favoriteOrders)
        {
            this.Id = id;
            this.Name = name;
            this.FavoriteOrders = favoriteOrders;
        }
    }

    public class FavoriteOrder
    {
        public string RestaurantName;
        public string OrderName;
        public string DeliveryAddress;
        public string[] OrderContents;
        public double Price;
        public FavoriteOrder(string restaurantName, string orderName, string deliveryAddress, string[] orderContents, double price)
        {
            this.RestaurantName = restaurantName;
            this.OrderName = orderName;
            this.DeliveryAddress = deliveryAddress;
            this.OrderContents = orderContents;
            this.Price = price;
        }
    }
}
