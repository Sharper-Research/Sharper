using Checkout.Interfaces;

namespace Checkout
{
    public class StockCatalogue : IStockCatalogue
    {
        public StockCatalogue()
        {
            Items = [new CatalogueItem('A', "A great thing", 50.0M),
                new CatalogueItem('B', "A good useful thing", 30.0M),
                new CatalogueItem('C', "An Ok thing", 20.0M),
                new CatalogueItem('D', "A cheapo thing", 15.0M)];
        }
        public List<CatalogueItem> Items { get; set; }
    }
}
