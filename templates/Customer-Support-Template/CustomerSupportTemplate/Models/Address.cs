namespace CustomerSupportTemplate.Models
{
    public class Address
    {
        public string Street1 { get; set; }

        public string Street2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}, {3} {4}", Street1, Street2 ?? string.Empty, City, State, Zip);
        }
    }
}