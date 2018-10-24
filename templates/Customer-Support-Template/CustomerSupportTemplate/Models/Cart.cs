using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Models
{
    public class Cart
    {
        public string Id { get; set; }

        public List<Product> Items { get; set; }
    }
}
