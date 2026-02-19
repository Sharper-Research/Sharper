namespace Checkout.Interfaces
{
    public interface ISpecialPrice
    {
        int NumberOfItemsToApply { get; init; }
        decimal PromotionPrice { get; init; }
        char Sku { get; init; }
        DateTime ValidDateRangeFrom { get; init; }
        DateTime ValidDateRangeTo { get; init; }

        void Deconstruct(out char Sku, out DateTime ValidDateRangeFrom, out DateTime ValidDateRangeTo, out decimal PromotionPrice, out int NumberOfItemsToApply);
        bool Equals(object? obj);
        bool Equals(SpecialPrice? other);
        int GetHashCode();
        string ToString();
    }
}