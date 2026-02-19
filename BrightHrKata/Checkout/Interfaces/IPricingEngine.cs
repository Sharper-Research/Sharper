using Checkout.Interfaces;

namespace Checkout
{
    public interface IPricingEngine
    {
        decimal GetSumOfAllNormalItemsPrices(IBasket basket);
        decimal GetSumOfAllSpecialPriceItems(IBasket basket);
    }
}