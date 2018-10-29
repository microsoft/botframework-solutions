using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Models
{
    public class Refund
    {
        public string Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public string OrderId { get; set; }

        public Product Product { get; set; }

        public RefundStatus Status { get; internal set; }

        public double RefundAmount { get; internal set; }
    }
}
