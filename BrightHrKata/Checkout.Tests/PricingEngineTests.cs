using Checkout.Interfaces;

namespace Checkout.Tests
{
    [TestClass]
    public class PricingEngineTests
    {
        private IPricingEngine _cut;
        private IBasket _basket;

        [TestInitialize]
        public void SetUp()
        {
            _cut = new PricingEngine(new SpecialPrices());
            _basket = new Basket();
        }

        #region Test_GetSumOfAllNormalItemsPrices
        [TestMethod]
        public void GIVEN_basket_with_no_items_WHEN_GetSumOfAllNormalItemsPrices_is_called_THEN_it_returns_0()
        {
            var result = _cut.GetSumOfAllNormalItemsPrices(_basket);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GIVEN_basket_with_no_items_WHEN_GetSumOfAllSpecialPriceItems_is_called_THEN_it_returns_0()
        {
            var result = _cut.GetSumOfAllSpecialPriceItems(_basket);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GIVEN_basket_with_single_item_not_on_special_priced_items_list_WHEN_GetSumOfAllNormalItemsPrices_is_called_THEN_it_returns_that_single_full_priced_items()
        {
            _basket.Add(new CatalogueItem('D', "Name", 15M));
            var result = _cut.GetSumOfAllNormalItemsPrices(_basket);
            Assert.AreEqual(15, result);
        }

        [TestMethod]
        public void GIVEN_basket_with_multiple_none_special_priced_items_WHEN_GetSumOfAllNormalItemsPrices_is_called_THEN_it_returns_sum_of_all_full_priced_items()
        {

            _basket.Add(new CatalogueItem('C', "Name", 20M));
            _basket.Add(new CatalogueItem('D', "Name", 15M));
            _basket.Add(new CatalogueItem('D', "Name", 15M));
            var result = _cut.GetSumOfAllNormalItemsPrices(_basket);
            Assert.AreEqual(50, result);
        }

        [TestMethod]
        public void GIVEN_basket_with_only_items_appearing_on_special_price_list_WHEN_GetSumOfAllNormalItemsPrices_is_called_THEN_it_returns_0()
        {
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('B', "Name", 30M));
            var result = _cut.GetSumOfAllNormalItemsPrices(_basket);
            Assert.AreEqual(0, result);
        }
#endregion

        #region Test_GetSumOfAllSpecialPriceItems
        [TestMethod]
        public void GIVEN_basket_with_exact_qualifying_number_of_special_price_items_WHEN_GetSumOfSpecialPriceItems_is_called_THEN_it_returns_special_price_for_those_items()
        {
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            var result = _cut.GetSumOfAllSpecialPriceItems(_basket);
            Assert.AreEqual(130, result);
        }

        [TestMethod]
        public void GIVEN_basket_contains_less_than_the_qualifying_number_of_special_price_items_WHEN_GetSumOfAllSpecialPriceItems_is_called_THEN_it_returns_full_price_for_those_items()
        {
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            var result = _cut.GetSumOfAllSpecialPriceItems(_basket);
            Assert.AreEqual(100, result);
        }

        [TestMethod]
        public void GIVEN_basket_contains_an_amount_of_items_greater_than_the_qualifying_amount_for_a_special_price_but_not_completely_divisible_by_the_number_required_WHEN_GetSumOfAllSpecialPriceItems_is_called_THEN_it_returns_correct_qualifying_prices_plus_full_price_for_remaining_items()
        {
            //8 items when special price qualify amount = 3 so special price * 2 + 2 * full price
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            var result = _cut.GetSumOfAllSpecialPriceItems(_basket);
            Assert.AreEqual(360, result);
        }

        [TestMethod]
        public void GIVEN_basket_with_items_on_special_price_list_but_not_enough_to_qualify_WHEN_GetSumOfAllSpecialPriceItems_is_called_THEN_the_full_price_for_each_item_is_returned()
        {
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            _basket.Add(new CatalogueItem('A', "Name", 50M));
            var result = _cut.GetSumOfAllSpecialPriceItems(_basket);
            Assert.AreEqual(100, result);
        }
        #endregion
    }
}
