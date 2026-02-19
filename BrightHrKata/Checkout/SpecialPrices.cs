using Checkout.Interfaces;

namespace Checkout
{
    public class SpecialPrices : ISpecialPrices
    {
        public List<SpecialPrice> Prices { get; }
        public SpecialPrices()
        {
            Prices = [new SpecialPrice('A', DateTime.Now, DateTime.Now.AddDays(1), 130M, 3),
                new SpecialPrice('B', DateTime.Now, DateTime.Now.AddDays(1), 45M, 2)];
        }
    }
}
