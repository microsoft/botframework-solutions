using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Models
{
    public class Order
    {
        public string Id { get; set; }

        public DateTime DatePlaced { get; set; }

        public string Phone { get; set; }

        public List<Product> Items { get; set; }

        public double Subtotal { get; set; }

        public double Tax { get; set; }

        public double Total { get; set; }

        public OrderStatus Status { get; set; }

        public string ShippingProvider { get; set; }

        public object TrackingNumber { get; set; }

        public object TrackingLink { get; set; }
    }
}
