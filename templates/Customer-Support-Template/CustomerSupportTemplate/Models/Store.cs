using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Models
{
    public class Store
    {
        public string Name { get; set; }

        public string Hours { get; set; }

        public string Phone { get; set; }

        public Address Address { get; set; }

        public List<Product> Catalog { get; set; }
    }
}
