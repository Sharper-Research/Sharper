using Checkout.Interfaces;

namespace Checkout
{
    public class Checkout(IStockCatalogue stockCatalogue, IPricingEngine pricingEngine) : ICheckout
    {
        private readonly IStockCatalogue _stockCatalogue = stockCatalogue;
        private readonly IPricingEngine _pricingEngine = pricingEngine;
        private IBasket _basket = new Basket();

        public decimal GetTotalCost()
        {
            return _pricingEngine.GetSumOfAllNormalItemsPrices(_basket) + _pricingEngine.GetSumOfAllSpecialPriceItems(_basket);
        }

        public void Scan(char sku)
        {
            var item = _stockCatalogue.Items.FirstOrDefault(x => x.Sku == sku);
            if (item != null)
            {
                _basket.Add(item);
            }
        }
    }
}
