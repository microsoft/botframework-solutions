using CustomerSupportTemplate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.ServiceClients
{
    public class DemoServiceClient : IServiceClient
    {
        public void CancelOrderByNumber(string orderNumber)
        {
            return;
        }

        public Account GetAccountById(string id)
        {
            return new Account()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "John Doe",
                Address = new Address()
                {
                    Street1 = "1234 Apple St",
                    Street2 = "Apt 405",
                    City = "Seattle",
                    State = "WA",
                    Zip = "98109"
                },
            };
        }

        public Cart GetCartById(string id)
        {
            return new Cart()
            {
                Items = new List<Product>()
                {
                    new Product()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Surface Go Signature Type Cover",
                        Url = "https://www.microsoft.com/en-us/p/surface-go-signature-type-cover/90kbccpw6fsv/0vzc?cid=msft_web_collection&activetab=pivot%3aoverviewtab",
                        ImageUrl = "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RE2clpU?ver=855b&q=90&m=6&h=270&w=270&b=%23FFFFFFFF&f=jpg&o=f&aim=true",
                        Price = 129.99
                    },
                    new Product()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Surface Pen - Platinum",
                        Url = "https://www.microsoft.com/en-us/p/surface-pen/8zl5c82qmg6b/7x3t?cid=msft_web_collection&activetab=pivot%3aoverviewtab",
                        ImageUrl = "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RE1LW0i?ver=38b3&q=90&m=6&h=270&w=270&b=%23FFFFFFFF&f=jpg&o=f&aim=true",
                        Price = 99.99
                    }
                }
            };
        }

        public Product GetItemById(string id)
        {
            return new Product()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Surface Pen - Platinum",
                Url = "https://www.microsoft.com/en-us/p/surface-pen/8zl5c82qmg6b/7x3t?cid=msft_web_collection&activetab=pivot%3aoverviewtab",
                ImageUrl = "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RE1LW0i?ver=38b3&q=90&m=6&h=270&w=270&b=%23FFFFFFFF&f=jpg&o=f&aim=true",
                Price = 99.99
            };
        }

        public Order GetOrderByNumber(string id)
        {
            return new Order()
            {
                Id = "5672939",
                DatePlaced = DateTime.Today.AddDays(-2),
                Items = new List<Product>()
                {
                    new Product()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Xbox One X 1TB Console",
                        Url = "https://www.microsoft.com/en-us/p/xbox-one-x-1tb-console/8mp3mpj68b7v",
                        ImageUrl = "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RWbGIz?ver=8530&q=90&m=6&h=423&w=752&b=%23FF171717&f=jpg&o=f&aim=true",
                        Price = 499.00
                    },
                    new Product()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Xbox Wireless Controller - Black",
                        Url = "https://www.microsoft.com/en-us/p/xbox-wireless-controller/8vcw8gln9vrf/ljvk?cid=msft_web_collection&activetab=pivot%3aoverviewtab",
                        ImageUrl = "https://compass-ssl.xbox.com/assets/2b/aa/2baa2c8b-4d4d-4f07-bba0-cebcd06de801.jpg?n=X1-Wireless-Controller-Black_Content-Placement-0_Accessory-Hub_740x417.jpg",
                        Price = 49.99
                    },
                },
                Subtotal = 499.00,
                Tax = 49.99,
                Total = 548.99,
                Phone = "1(206)555-1234",
                Status = OrderStatus.Shipped,
                ShippingProvider = "USPS",
                TrackingNumber = "123456789",
                TrackingLink = "https://tools.usps.com/go/TrackConfirmAction",
            };
        }

        public List<Promo> GetPromoCodes()
        {
            return new List<Promo>()
            {
                new Promo()
                {
                    Name = "FREE",
                    Code = "FREE",
                    Description = "Get free shipping on all orders over $50!"
                },
                new Promo()
                {
                    Name = "SCHOOL18",
                    Code = "SCHOOL18",
                    Description = "Get 20% off select items during our back-to-school sale!"
                },
                new Promo()
                {
                    Name = "ANNIVERSARY",
                    Code = "ANNIVERSARY",
                    Description = "Get 10% off all items during our Anniversary sale!"
                }
            };
        }

        public List<Promo> GetPromoCodesByCart(string id)
        {
            return new List<Promo>()
            {
                new Promo()
                {
                    Name = "FREE",
                    Code = "FREE",
                    Description = "Get free shipping on all orders over $50!"
                },
                 new Promo()
                {
                    Name = "ANNIVERSARY",
                    Code = "ANNIVERSARY",
                    Description = "Get 10% off all items during our Anniversary sale!"
                }
            };
        }

        public Refund GetRefundStatus(string orderNumber)
        {
            return new Refund()
            {
                Id = "3748594",
                CreatedDate = DateTime.Today.AddDays(-5),
                Status = RefundStatus.Processing,
                Product = new Product()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Surface Keyboard",
                    Url = "https://www.microsoft.com/en-us/p/surface-keyboard/8r3rqvvflp4k/dsng?activetab=pivot%3aoverviewtab",
                    ImageUrl = "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RE1J7Qy?ver=6487&q=90&m=6&h=270&w=270&b=%23FFFFFFFF&f=jpg&o=f&aim=true",
                    Price = 99.99
                },
                RefundAmount = 109.99
            };
        }

        public List<Store> GetStoresByZipCode(string zip)
        {
            return new List<Store>()
            {
                new Store()
                {
                    Name = "Microsoft Store - Bellevue Square",
                    Address = new Address()
                    {
                        Street1 = "116 Bellevue Way NE",
                        City = "Bellevue",
                        State = "WA", 
                        Zip = "98004"
                    },
                    Hours = "Monday - Saturday 9:30AM - 9:30PM, Sunday 11AM - 7PM",
                    Phone = "1(425)519-3580"
                },
                new Store()
                {
                    Name = "Microsoft Store - University Village",
                    Address = new Address()
                    {
                        Street1 = "2624 NE University Village St",
                        City = "Seattle",
                        State = "WA",
                        Zip = "98105"
                    },
                    Hours = "Monday - Saturday 9:30AM - 9PM, Sunday 11AM - 6PM",
                    Phone = "1(206)834-0680"
                },
                new Store()
                {
                    Name = "Microsoft Store - Westfield Southcenter",
                    Address = new Address()
                    {
                        Street1 = "Westfield Southcenter, 2800 Southcenter Mall, Seattle, WA 98188",
                        City = "Bellevue",
                        State = "WA",
                        Zip = "98004"
                    },
                    Hours = "Monday - Saturday 10AM - 9PM, Sunday 11AM - 7PM",
                    Phone = "1(855)270-6581"
                }
            };
        }

        public List<Store> GetStoresWithItemByZip(string zip, string item)
        {
            return new List<Store>()
            {
                new Store()
                {
                    Name = "Microsoft Store - Bellevue Square",
                    Address = new Address()
                    {
                        Street1 = "116 Bellevue Way NE",
                        City = "Bellevue",
                        State = "WA",
                        Zip = "98004"
                    },
                    Hours = "Monday - Saturday 9:30AM - 9:30PM, Sunday 11AM - 7PM",
                    Phone = "1(425)519-3580"
                },
                new Store()
                {
                    Name = "Microsoft Store - University Village",
                    Address = new Address()
                    {
                        Street1 = "2624 NE University Village St",
                        City = "Seattle",
                        State = "WA",
                        Zip = "98105"
                    },
                    Hours = "Monday - Saturday 9:30AM - 9PM, Sunday 11AM - 6PM",
                    Phone = "1(206)834-0680"
                },
            };
        }

        public void HoldItem(object storeId, object itemId, object accountId)
        {
            return;
        }

        public void SendPasswordResetEmail(string id)
        {
            return;
        }

        public void UpdateUserContactInfo(Account info)
        {
            return;
        }
    }
}
