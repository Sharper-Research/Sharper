namespace Checkout.Interfaces
{
    public interface IBasket
    {
        void Add(ICatalogueItem item);
        void Remove(ICatalogueItem item);

        IList<ICatalogueItem> Items { get; }
    }
}