namespace Checkout.Interfaces
{
    public interface ICheckout
    {
        decimal GetTotalCost();
        void Scan(char sku);
    }
}