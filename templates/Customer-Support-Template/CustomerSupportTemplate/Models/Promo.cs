using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Models
{
    public class Promo
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public List<Product> Products { get; set; }
    }
}
