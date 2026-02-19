namespace Checkout
{
    public record QualifyingItemsSummary(char Sku, int BasketAmount, int NumberToQualifyForPricing, decimal SpecialPrice, decimal individualPrice);
}
