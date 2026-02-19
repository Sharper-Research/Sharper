namespace Checkout.Tests
{
    [TestClass]
    public class CheckoutTests
    {
        private Checkout? _cut;

        [TestInitialize]
        public void SetUp()
        {
            _cut = new Checkout(new StockCatalogue(), new PricingEngine(new SpecialPrices()));
        }

        [TestMethod]
        public void Empty_basket_get_total_returns_no_cost()
        {
            var result = _cut?.GetTotalCost();
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetTotalCost_returns_price_for_single_A_product_when_one_scanned()
        {
            _cut?.Scan('A');
            var result = _cut?.GetTotalCost();
            Assert.AreEqual(50, result);
        }

        [TestMethod]
        public void GetTotalCost_returns_sum_prices_for_single_A_product_and_single_B_product_when_one_of_each_scanned()
        {
            _cut?.Scan('A');
            _cut?.Scan('B');
            var result = _cut?.GetTotalCost();
            Assert.AreEqual(80, result);
        }

        [TestMethod]
        public void GetTotalCost_returns_sum_prices_for_single_A_product_and_single_B_product_and_single_C_product_when_one_of_each_scanned()
        {
            _cut?.Scan('A');
            _cut?.Scan('B');
            _cut?.Scan('C');
            var result = _cut?.GetTotalCost();
            Assert.AreEqual(100, result);
        }

        [TestMethod]
        public void Scanning_invalid_item_does_not_throws_known_exception()
        {
            try
            {
                _cut?.Scan('E');
            }
            catch (Exception ex)
            {

                Assert.Fail($"No exception expected but thrown with message: '{ex.Message}'");
            }
        }
    }
}