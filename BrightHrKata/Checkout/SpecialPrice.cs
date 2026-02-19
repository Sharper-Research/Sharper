
using Checkout.Interfaces;

namespace Checkout
{
    public record SpecialPrice(char Sku, DateTime ValidDateRangeFrom, DateTime ValidDateRangeTo, decimal PromotionPrice, int NumberOfItemsToApply) 
        : ISpecialPrice;
}