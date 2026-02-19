using Checkout.Interfaces;

namespace Checkout
{
    public record CatalogueItem(char Sku, string Name, decimal SalesPrice) : ICatalogueItem;
}