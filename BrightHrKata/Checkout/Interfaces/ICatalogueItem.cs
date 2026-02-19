namespace Checkout.Interfaces
{
    public interface ICatalogueItem
    {
        string Name { get; init; }
        decimal SalesPrice { get; init; }
        char Sku { get; init; }

        void Deconstruct(out char Sku, out string Name, out decimal SalesPrice);
        bool Equals(CatalogueItem? other);
        bool Equals(object? obj);
        int GetHashCode();
        string ToString();
    }
}