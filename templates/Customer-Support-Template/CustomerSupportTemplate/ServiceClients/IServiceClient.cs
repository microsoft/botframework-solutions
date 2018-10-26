using CustomerSupportTemplate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.ServiceClients
{
    public interface IServiceClient
    {
        Account GetAccountById(string id);

        void SendPasswordResetEmail(string id);

        void UpdateUserContactInfo(Account info);

        Order GetOrderByNumber(string id);

        void CancelOrderByNumber(string orderNumber);

        Cart GetCartById(string id);

        List<Store> GetStoresByZipCode(string zip);

        Refund GetRefundStatus(string orderNumber);

        Product GetItemById(string id);

        List<Store> GetStoresWithItemByZip(string zip, string item);

        void HoldItem(object storeId, object itemId, object accountId);

        List<Promo> GetPromoCodes();

        List<Promo> GetPromoCodesByCart(string id);
    }
}
