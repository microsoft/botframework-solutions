using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Models
{
    public class Product
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public double Price { get; set; }
    }
}
