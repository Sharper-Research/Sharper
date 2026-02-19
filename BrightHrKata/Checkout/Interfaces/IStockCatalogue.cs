namespace Checkout.Interfaces
{
    public interface IStockCatalogue
    {
        List<CatalogueItem> Items { get; set; }
    }
}