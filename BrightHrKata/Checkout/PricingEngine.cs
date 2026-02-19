using Checkout.Interfaces;

namespace Checkout
{
    public class PricingEngine(ISpecialPrices specialPrices) : IPricingEngine
    {
        private readonly ISpecialPrices _specialPrices = specialPrices;
        private decimal _runningTotal = 0M;

        public decimal GetSumOfAllSpecialPriceItems(IBasket basket)
        {
            var localSum = 0M;
            var specialPrices = GetCurrentSpecialPrices()
            .ToList();

            var applicableBasketItems = basket
                .Items
                .Where(x => specialPrices.Select(y => y.Sku)
                .Distinct()
                .Contains(x.Sku))
                .ToList();


            var query = (from specialPrice in specialPrices
                         join applicableItem in applicableBasketItems on specialPrice.Sku equals applicableItem.Sku
                         select new QualifyingItemsSummary
                         (
                             applicableItem.Sku,
                             applicableBasketItems.Count(x => x.Sku == applicableItem.Sku),
                             specialPrice.NumberOfItemsToApply,
                             specialPrice.PromotionPrice,
                             applicableItem.SalesPrice

                         ))
                        .Distinct()
                        .ToList();

            foreach (var item in query)
            {
                if (item.BasketAmount >= item.NumberToQualifyForPricing)
                {
                    localSum += item.BasketAmount / item.NumberToQualifyForPricing * item.SpecialPrice;
                    if (item.NumberToQualifyForPricing % item.BasketAmount > 0)
                    {
                        localSum += item.BasketAmount % item.NumberToQualifyForPricing * item.individualPrice;
                    }
                }
                else
                {
                    localSum += item.BasketAmount * item.individualPrice;
                }
            }

            return localSum;
        }

        public decimal GetSumOfAllNormalItemsPrices(IBasket basket)
        {
            var localSum = 0M;
            var specialPrices = GetCurrentSpecialPrices();

            foreach (var item in basket.Items
                        .Where(x => !specialPrices
                                    .Select(y => y.Sku)
                                    .Distinct()
                                    .Contains(x.Sku)))
            {
                localSum += item.SalesPrice;
            }

            return localSum;
        }

        private List<SpecialPrice> GetCurrentSpecialPrices()
        {
            return _specialPrices
                .Prices
                .Where(x => x.ValidDateRangeFrom < DateTime.Now && x.ValidDateRangeTo > DateTime.Now)
                .ToList();
        }
    }
}
